using DemoApp.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Pipeline;
using TensorStack.Common.Tensor;
using TensorStack.Providers;
using TensorStack.StableDiffusion.Common;
using TensorStack.StableDiffusion.Enums;
using TensorStack.StableDiffusion.Models;
using TensorStack.StableDiffusion.Pipelines.Flux;
using TensorStack.StableDiffusion.Pipelines.LatentConsistency;
using TensorStack.StableDiffusion.Pipelines.Nitro;
using TensorStack.StableDiffusion.Pipelines.StableCascade;
using TensorStack.StableDiffusion.Pipelines.StableDiffusion;
using TensorStack.StableDiffusion.Pipelines.StableDiffusion2;
using TensorStack.StableDiffusion.Pipelines.StableDiffusion3;
using TensorStack.StableDiffusion.Pipelines.StableDiffusionXL;
using TensorStack.WPF;

namespace DemoApp.Services
{
    public class DiffusionService : ServiceBase, IDiffusionService
    {
        private readonly Settings _settings;
        private PipelineModel _currentPipeline;
        private ControlNetModel _currentControlModel;
        private IPipeline<ImageTensor, GenerateOptions, GenerateProgress> _diffusionPipeline;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _isExecuting;
        private GenerateOptions _defaultOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiffusionService"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public DiffusionService(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        public PipelineModel Pipeline => _currentPipeline;

        /// <summary>
        /// Gets the default options.
        /// </summary>
        public GenerateOptions DefaultOptions => _defaultOptions;

        /// <summary>
        /// Gets a value indicating whether this instance is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get { return _isLoaded; }
            private set { SetProperty(ref _isLoaded, value); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is loading.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); NotifyPropertyChanged(nameof(CanCancel)); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is executing.
        /// </summary>
        public bool IsExecuting
        {
            get { return _isExecuting; }
            private set { SetProperty(ref _isExecuting, value); NotifyPropertyChanged(nameof(CanCancel)); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can cancel.
        /// </summary>
        public bool CanCancel => _isLoading || _isExecuting;


        /// <summary>
        /// Load the upscale pipeline
        /// </summary>
        /// <param name="config">The configuration.</param>
        public async Task LoadAsync(PipelineModel pipeline)
        {
            try
            {
                IsLoaded = false;
                IsLoading = true;
                using (_cancellationTokenSource = new CancellationTokenSource())
                {
                    var cancellationToken = _cancellationTokenSource.Token;
                    if (_diffusionPipeline != null)
                        await _diffusionPipeline.UnloadAsync(cancellationToken);
                    if (_currentControlModel != null)
                        await _currentControlModel.UnloadAsync();

                    var model = pipeline.DiffusionModel;
                    var device = pipeline.Device;
                    var provider = device.GetProvider();

                    _currentPipeline = pipeline;
                    _currentControlModel = _currentPipeline.ControlModel is null
                        ? default
                        : ControlNetModel.FromFile(_currentPipeline.ControlModel.Path, provider);

                    if (model.PipelineType == PipelineType.Flux)
                    {
                        var fluxPipeline = FluxPipeline.FromFolder(model.Path, model.ModelType, provider);
                        _diffusionPipeline = fluxPipeline;
                        _defaultOptions = fluxPipeline.DefaultOptions;
                    }
                    else if (model.PipelineType == PipelineType.LatentConsistency)
                    {
                        var latentConsistencyPipeline = LatentConsistencyPipeline.FromFolder(model.Path, model.ModelType, provider);
                        _diffusionPipeline = latentConsistencyPipeline;
                        _defaultOptions = latentConsistencyPipeline.DefaultOptions;
                    }
                    else if (model.PipelineType == PipelineType.StableCascade)
                    {
                        var stableCascadePipeline = StableCascadePipeline.FromFolder(model.Path, model.ModelType, provider);
                        _diffusionPipeline = stableCascadePipeline;
                        _defaultOptions = stableCascadePipeline.DefaultOptions;
                    }
                    else if (model.PipelineType == PipelineType.StableDiffusion)
                    {
                        var stableDiffusionPipeline = StableDiffusionPipeline.FromFolder(model.Path, model.ModelType, provider);
                        _diffusionPipeline = stableDiffusionPipeline;
                        _defaultOptions = stableDiffusionPipeline.DefaultOptions;
                    }
                    else if (model.PipelineType == PipelineType.StableDiffusion2)
                    {
                        var stableDiffusion2Pipeline = StableDiffusion2Pipeline.FromFolder(model.Path, model.ModelType, provider);
                        _diffusionPipeline = stableDiffusion2Pipeline;
                        _defaultOptions = stableDiffusion2Pipeline.DefaultOptions;
                    }
                    else if (model.PipelineType == PipelineType.StableDiffusion3)
                    {
                        var stableDiffusion3Pipeline = StableDiffusion3Pipeline.FromFolder(model.Path, model.ModelType, provider);
                        _diffusionPipeline = stableDiffusion3Pipeline;
                        _defaultOptions = stableDiffusion3Pipeline.DefaultOptions;
                    }
                    else if (model.PipelineType == PipelineType.StableDiffusionXL)
                    {
                        var stableDiffusionXLPipeline = StableDiffusionXLPipeline.FromFolder(model.Path, model.ModelType, provider);
                        _diffusionPipeline = stableDiffusionXLPipeline;
                        _defaultOptions = stableDiffusionXLPipeline.DefaultOptions;
                    }
                    else if (model.PipelineType == PipelineType.Nitro)
                    {
                        var nitroPipeline = NitroPipeline.FromFolder(model.Path, 512, model.ModelType, provider);
                        _diffusionPipeline = nitroPipeline;
                        _defaultOptions = nitroPipeline.DefaultOptions;
                    }
                    await Task.Run(() => _diffusionPipeline.LoadAsync(cancellationToken), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _diffusionPipeline?.Dispose();
                _diffusionPipeline = null;
                _defaultOptions = null;
                _currentPipeline = null;
                throw;
            }
            finally
            {
                IsLoaded = true;
                IsLoading = false;
            }
        }


        /// <summary>
        /// Execute the upscaler
        /// </summary>
        /// <param name="request">The request.</param>
        public async Task<ImageTensor> ExecuteAsync(ImageGenerateOptions options)
        {
            try
            {
                IsExecuting = true;
                using (_cancellationTokenSource = new CancellationTokenSource())
                {
                    var generateOptions = _defaultOptions with
                    {
                        Seed = options.Seed,
                        Prompt = options.Prompt,
                        NegativePrompt = options.NegativePrompt,
                        Scheduler = options.Scheduler,
                        Steps = options.Steps,
                        Width = options.Width,
                        Height = options.Height,
                        GuidanceScale = options.GuidanceScale,
                        Strength = options.Strength,
                        ControlNetStrength = options.ControlNetStrength,
                        InputImage = options.InputImage,
                        InputControlImage = options.InputControlImage,
                        ControlNet = _currentControlModel
                    };

                    return await Task.Run(() => _diffusionPipeline.RunAsync(generateOptions, cancellationToken: _cancellationTokenSource.Token));
                }
            }
            finally
            {
                IsExecuting = false;
            }
        }


        /// <summary>
        /// Cancel the running task (Load or Execute)
        /// </summary>
        public async Task CancelAsync()
        {
            await _cancellationTokenSource.SafeCancelAsync();
        }


        /// <summary>
        /// Unload the pipeline
        /// </summary>
        public async Task UnloadAsync()
        {
            await _cancellationTokenSource.SafeCancelAsync();

            if (_currentControlModel != null)
            {
                await _currentControlModel.UnloadAsync();
                _currentControlModel.Dispose();
                _currentControlModel = null;
            }

            if (_diffusionPipeline != null)
            {
                await _diffusionPipeline.UnloadAsync();
                _diffusionPipeline.Dispose();
                _diffusionPipeline = null;
            }

            _currentPipeline = null;
            IsLoaded = false;
            IsLoading = false;
            IsExecuting = false;
        }
    }


    public interface IDiffusionService
    {
        PipelineModel Pipeline { get; }
        GenerateOptions DefaultOptions { get; }
        bool IsLoaded { get; }
        bool IsLoading { get; }
        bool IsExecuting { get; }
        bool CanCancel { get; }
        Task LoadAsync(PipelineModel pipeline);
        Task UnloadAsync();
        Task CancelAsync();
        Task<ImageTensor> ExecuteAsync(ImageGenerateOptions options);
    }


    public record ImageGenerateOptions : BaseRecord
    {
        private int _width;
        private int _height;
        private int _seed;
        private SchedulerType _scheduler;
        private float _guidanceScale;
        private string _prompt;
        private string _negativePrompt;
        private int _steps;
        private float _strength;
        private float _controlNetStrength;
        private ImageTensor _inputImage;
        private ImageTensor _inputControlImage;

        public int Width
        {
            get { return _width; }
            set { SetProperty(ref _width, value); }
        }

        public int Height
        {
            get { return _height; }
            set { SetProperty(ref _height, value); }
        }

        public int Seed
        {
            get { return _seed; }
            set { SetProperty(ref _seed, value); }
        }

        public SchedulerType Scheduler
        {
            get { return _scheduler; }
            set { SetProperty(ref _scheduler, value); }
        }

        public float GuidanceScale
        {
            get { return _guidanceScale; }
            set { SetProperty(ref _guidanceScale, value); }
        }

        public string Prompt
        {
            get { return _prompt; }
            set { SetProperty(ref _prompt, value); }
        }

        public string NegativePrompt
        {
            get { return _negativePrompt; }
            set { SetProperty(ref _negativePrompt, value); }
        }

        public int Steps
        {
            get { return _steps; }
            set { SetProperty(ref _steps, value); }
        }

        public float Strength
        {
            get { return _strength; }
            set { SetProperty(ref _strength, value); }
        }

        public float ControlNetStrength
        {
            get { return _controlNetStrength; }
            set { SetProperty(ref _controlNetStrength, value); }
        }

        public ImageTensor InputImage
        {
            get { return _inputImage; }
            set { SetProperty(ref _inputImage, value); }
        }

        public ImageTensor InputControlImage
        {
            get { return _inputControlImage; }
            set { SetProperty(ref _inputControlImage, value); }
        }
    }
}
