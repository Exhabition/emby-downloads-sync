using System.Net;
using Emby.ApiClient.Api;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Domain.Exceptions;
using EmbyDownloadsSync.Infrastructure.Services;
using Moq;
using RestSharp;

namespace EmbyDownloadsSync.Tests.Infrastructure.Services;

public class JobServiceTests
{
	private readonly Mock<ISyncServiceApi> _mockSyncServiceApi;
	private readonly JobService _jobService;

	public JobServiceTests()
	{
		_mockSyncServiceApi = new Mock<ISyncServiceApi>();
		_jobService = new JobService(_mockSyncServiceApi.Object);
	}

	[Fact]
	public async Task GetJobsByDeviceId_ThrowsArgumentException_WhenDeviceIdIsNull()
	{
		await Assert.ThrowsAnyAsync<ArgumentException>(() => _jobService.GetJobsByDeviceId(null!));
	}

	[Fact]
	public async Task GetJobsByDeviceId_ThrowsArgumentException_WhenDeviceIdIsEmpty()
	{
		await Assert.ThrowsAnyAsync<ArgumentException>(() => _jobService.GetJobsByDeviceId(""));
	}

	[Fact]
	public async Task GetJobsByDeviceId_ThrowsArgumentException_WhenDeviceIdIsWhitespace()
	{
		await Assert.ThrowsAnyAsync<ArgumentException>(() => _jobService.GetJobsByDeviceId("   "));
	}

	[Fact]
	public async Task GetJobsByDeviceId_ReturnsFilteredJobs_WhenJobsExist()
	{
		// Arrange
		var jobs = new List<SyncJob>
		{
			new() { TargetId = "device-1", Name = "Job 1" },
			new() { TargetId = "device-2", Name = "Job 2" },
			new() { TargetId = "device-1", Name = "Job 3" }
		};

		SetupGetSyncJobsSuccess(new QueryResultSyncJob { Items = jobs });

		// Act
		var result = await _jobService.GetJobsByDeviceId("device-1");

		// Assert
		Assert.Equal(2, result.Count);
		Assert.All(result, job => Assert.Equal("device-1", job.TargetId));
	}

	[Fact]
	public async Task GetJobsByDeviceId_ReturnsEmptyList_WhenNoMatchingJobs()
	{
		// Arrange
		var jobs = new List<SyncJob>
		{
			new() { TargetId = "device-1", Name = "Job 1" }
		};

		SetupGetSyncJobsSuccess(new QueryResultSyncJob { Items = jobs });

		// Act
		var result = await _jobService.GetJobsByDeviceId("device-999");

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetJobsByDeviceId_ReturnsEmptyList_WhenItemsIsNull()
	{
		// Arrange
		SetupGetSyncJobsSuccess(new QueryResultSyncJob { Items = null });

		// Act
		var result = await _jobService.GetJobsByDeviceId("device-1");

		// Assert
		Assert.Empty(result);
	}

	[Fact]
	public async Task GetJobs_ReturnsData_WhenResponseIsSuccessful()
	{
		// Arrange
		var expectedData = new QueryResultSyncJob
		{
			Items = [new SyncJob { Name = "Test Job" }]
		};

		SetupGetSyncJobsSuccess(expectedData);

		// Act
		var result = await _jobService.GetJobs();

		// Assert
		Assert.Same(expectedData, result);
	}

	[Fact]
	public async Task GetJobs_ThrowsEmbyApiException_WhenResponseIsNotSuccessful()
	{
		// Arrange
		SetupGetSyncJobsFailure(HttpStatusCode.InternalServerError, "Server error");

		// Act
		var exception = await Assert.ThrowsAsync<EmbyApiException>(() => _jobService.GetJobs());

		// Assert
		Assert.Contains("Failed to fetch sync jobs", exception.Message);
		Assert.Contains("InternalServerError", exception.Message);
		Assert.Contains("Server error", exception.Message);
	}

	[Fact]
	public async Task GetJobs_ThrowsEmbyApiException_WhenDataIsNull()
	{
		// Arrange
		SetupGetSyncJobsSuccess(null!);

		// Act
		var exception = await Assert.ThrowsAsync<EmbyApiException>(() => _jobService.GetJobs());

		// Assert
		Assert.Contains("sync jobs data was null", exception.Message);
	}

	[Fact]
	public async Task CreateDuplicateJob_ThrowsArgumentException_WhenTargetIdIsNull()
	{
		// Arrange
		var syncJob = new SyncJob();

		// Act & Assert
		await Assert.ThrowsAnyAsync<ArgumentException>(() => _jobService.CreateDuplicateJob(syncJob, null!));
	}

	[Fact]
	public async Task CreateDuplicateJob_ThrowsArgumentException_WhenTargetIdIsEmpty()
	{
		// Arrange
		var syncJob = new SyncJob();

		// Act & Assert
		await Assert.ThrowsAnyAsync<ArgumentException>(() => _jobService.CreateDuplicateJob(syncJob, ""));
	}

	[Fact]
	public async Task CreateDuplicateJob_ThrowsArgumentException_WhenTargetIdIsWhitespace()
	{
		// Arrange
		var syncJob = new SyncJob();

		// Act & Assert
		await Assert.ThrowsAnyAsync<ArgumentException>(() => _jobService.CreateDuplicateJob(syncJob, "   "));
	}

	[Fact]
	public async Task CreateDuplicateJob_ReturnsResult_WhenResponseIsSuccessful()
	{
		// Arrange
		var syncJob = new SyncJob { Name = "Original Job" };
		var expectedResult = new SyncJobCreationResult { Job = new SyncJob { Name = "Created Job" } };

		SetupPostSyncJobsSuccess(expectedResult);

		// Act
		var result = await _jobService.CreateDuplicateJob(syncJob, "target-device");

		// Assert
		Assert.Same(expectedResult, result);
	}

	[Fact]
	public async Task CreateDuplicateJob_ThrowsEmbyApiException_WhenResponseIsNotSuccessful()
	{
		// Arrange
		var syncJob = new SyncJob { Name = "Test Job" };

		SetupPostSyncJobsFailure(HttpStatusCode.BadRequest, "Invalid request");

		// Act
		var exception =
			await Assert.ThrowsAsync<EmbyApiException>(() => _jobService.CreateDuplicateJob(syncJob, "target-device"));

		// Assert
		Assert.Contains("Failed to create sync job", exception.Message);
		Assert.Contains("BadRequest", exception.Message);
		Assert.Contains("Invalid request", exception.Message);
	}

	[Fact]
	public async Task CreateDuplicateJob_ThrowsEmbyApiException_WhenDataIsNull()
	{
		// Arrange
		var syncJob = new SyncJob { Name = "Test Job" };

		SetupPostSyncJobsSuccess(null!);

		// Act
		var exception =
			await Assert.ThrowsAsync<EmbyApiException>(() => _jobService.CreateDuplicateJob(syncJob, "target-device"));

		// Assert
		Assert.Contains("created sync job data was null", exception.Message);
	}

	private void SetupGetSyncJobsSuccess(QueryResultSyncJob data)
	{
		var response = CreateSuccessResponse(data);
		_mockSyncServiceApi
			.Setup(api => api.GetSyncJobs())
			.ReturnsAsync(response);
	}

	private void SetupGetSyncJobsFailure(HttpStatusCode statusCode, string errorMessage)
	{
		var response = CreateFailureResponse<QueryResultSyncJob>(statusCode, errorMessage);
		_mockSyncServiceApi
			.Setup(api => api.GetSyncJobs())
			.ReturnsAsync(response);
	}

	private void SetupPostSyncJobsSuccess(SyncJobCreationResult data)
	{
		var response = CreateSuccessResponse(data);
		_mockSyncServiceApi
			.Setup(api => api.PostSyncJobs(It.IsAny<SyncJobRequest>()))
			.ReturnsAsync(response);
	}

	private void SetupPostSyncJobsFailure(HttpStatusCode statusCode, string errorMessage)
	{
		var response = CreateFailureResponse<SyncJobCreationResult>(statusCode, errorMessage);
		_mockSyncServiceApi
			.Setup(api => api.PostSyncJobs(It.IsAny<SyncJobRequest>()))
			.ReturnsAsync(response);
	}

	private static RestResponse<T> CreateSuccessResponse<T>(T data)
	{
		return new RestResponse<T>(new RestRequest())
		{
			Data = data,
			StatusCode = HttpStatusCode.OK,
			IsSuccessStatusCode = true,
			ResponseStatus = ResponseStatus.Completed
		};
	}

	private static RestResponse<T> CreateFailureResponse<T>(HttpStatusCode statusCode, string errorMessage)
	{
		return new RestResponse<T>(new RestRequest())
		{
			Data = default,
			StatusCode = statusCode,
			ErrorMessage = errorMessage,
			IsSuccessStatusCode = false,
			ResponseStatus = ResponseStatus.Completed
		};
	}
}