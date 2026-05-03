using AIQ.Exports.GoogleSlides.Actions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Slides.v1;
using Google.Apis.Slides.v1.Data;
using DriveData = Google.Apis.Drive.v3.Data;
using static AIQ.Exports.GoogleSlides.GoogleSlidesConstants;

namespace AIQ.Exports.GoogleSlides;
public class GoogleSlidesService
{
    private readonly SlidesService _slidesService;
    private readonly DriveService _driveService;
    private readonly string _presentationId;
     private const string _slidesScope = SlidesScope;
     private const string _driveScope = DriveScope;

    public GoogleSlidesService(string serviceAccountJsonPath, string presentationId)
    {
        string json = File.ReadAllText(serviceAccountJsonPath);
        var credential = GoogleCredential.FromJson(json)
            .CreateScoped([_slidesScope, _driveScope]);

        _slidesService = new SlidesService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
             ApplicationName = ApplicationName
         });

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = ApplicationName
        });

        _presentationId = presentationId;
    }


    public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var apiTask = _slidesService
                .Presentations
                .Get(_presentationId)
                .ExecuteAsync(); 

            var completed = await Task.WhenAny(
                apiTask,
                Task.Delay(Timeout.Infinite, cancellationToken)
            );

            if (completed != apiTask)
                // cancellation triggered
                throw new OperationCanceledException(cancellationToken);

            return await apiTask != null; 
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> ExecuteBatchAsync(List<IGoogleSlidesAction> actions, CancellationToken cancellationToken = default)
    {
        if (actions.Count == 0) return 0;

        var requests = actions.Select(action => action.ToRequest(_presentationId)).ToList();
        var batchRequest = new BatchUpdatePresentationRequest { Requests = requests };

        var response = await _slidesService.Presentations.BatchUpdate(batchRequest, _presentationId).ExecuteAsync(cancellationToken);

        return response.Replies?.Sum(r =>
            (r.ReplaceAllText?.OccurrencesChanged ?? 0) +
            (r.ReplaceAllShapesWithImage?.OccurrencesChanged ?? 0)) ?? 0;
    }

    public async Task<BatchUpdatePresentationResponse> ExecuteBatchWithResponseAsync(
        List<IGoogleSlidesAction> actions,
        CancellationToken cancellationToken = default)
    {
        if (actions.Count == 0) return new BatchUpdatePresentationResponse();

        var requests = actions.Select(action => action.ToRequest(_presentationId)).ToList();
        var batchRequest = new BatchUpdatePresentationRequest { Requests = requests };

        return await _slidesService.Presentations.BatchUpdate(batchRequest, _presentationId)
            .ExecuteAsync(cancellationToken);
    }

    public async Task<Presentation> GetPresentationAsync(CancellationToken cancellationToken = default)
    {
        return await _slidesService.Presentations.Get(_presentationId).ExecuteAsync(cancellationToken);
    }

    public async Task<string> CopyPresentationAsync(string? name = null, CancellationToken cancellationToken = default)
    {
        var fileBody = new DriveData.File();
        if (!string.IsNullOrWhiteSpace(name))
        {
            fileBody.Name = name;
        }

        var copyRequest = _driveService.Files.Copy(fileBody, _presentationId);
        var file = await copyRequest.ExecuteAsync(cancellationToken);
        return file.Id;
    }
}