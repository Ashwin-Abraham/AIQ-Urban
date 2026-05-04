using Google.Apis.Drive.v3;
using Google.Apis.Slides.v1.Data;

namespace AIQ.Exports.GoogleSlides.Actions;
public interface IGoogleSlidesAction
{
    Request ToRequest(Presentation presentation);

    Task UploadRequestMaterialAsync(DriveService driveService) => Task.CompletedTask;

    Task DeleteRequestMaterialAsync(DriveService driveService) => Task.CompletedTask;
}