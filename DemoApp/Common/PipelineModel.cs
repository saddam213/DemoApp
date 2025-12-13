using TensorStack.Common;
using TensorStack.WPF;

namespace DemoApp.Common
{
    public class PipelineModel : BaseModel
    {
        public Device Device { get; init; }
        public DiffusionModel DiffusionModel { get; init; }
        public DiffusionControlNetModel ControlModel { get; init; }
        public ExtractorModel ExtractorModel { get; init; }
        public UpscaleModel UpscaleModel { get; init; }
        public DetectModel DetectModel { get; init; }
        public TextModel TextModel { get; init; }
        public TranscribeModel TranscribeModel { get; init; }
        public NarrateModel NarrateModel { get; init; }
    }
}
