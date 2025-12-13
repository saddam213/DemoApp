using System;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Pipeline;
using TensorStack.Common.Tensor;
using DemoApp.Common;
using TensorStack.Providers;
using TensorStack.TextGeneration.Common;
using TensorStack.TextGeneration.Pipelines.Supertonic;

namespace DemoApp.Services
{
    public class NarrateService : ServiceBase, INarrateService
    {
        private readonly Settings _settings;
        private IPipeline _narratePipeline;
        private PipelineModel _currentPipeline;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isLoaded;
        private bool _isLoading;
        private bool _isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="NarrateService"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public NarrateService(Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Gets the pipeline.
        /// </summary>
        public PipelineModel Pipeline => _currentPipeline;

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
                    if (_narratePipeline != null)
                        await _narratePipeline.UnloadAsync(cancellationToken);

                    _currentPipeline = pipeline;
                    var device = pipeline.Device;
                    var model = pipeline.NarrateModel;
                    var provider = device.GetProvider();
                    _narratePipeline = SupertonicPipeline.Create(model.Path, provider);
                    await Task.Run(() => _narratePipeline.LoadAsync(cancellationToken), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _narratePipeline?.Dispose();
                _narratePipeline = null;
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
        /// Execute the pipeline.
        /// </summary>
        /// <param name="options">The options.</param>
        public async Task<AudioTensor> ExecuteAsync(NarrateRequest options)
        {
            try
            {
                IsExecuting = true;
                using (_cancellationTokenSource = new CancellationTokenSource())
                {
                    var pipeline = _narratePipeline as IPipeline<AudioTensor, SupertonicOptions, GenerateProgress>;
                    var pipelineOptions = new SupertonicOptions
                    {
                        TextInput = options.InputText,
                        VoiceStyle = options.VoiceStyle,
                        Steps = options.Steps,
                        Speed = options.Speed,
                        SilenceDuration = options.SilenceDuration,
                        Seed = options.Seed,
                    };

                    return await pipeline.RunAsync(pipelineOptions, cancellationToken: _cancellationTokenSource.Token);
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
            if (_narratePipeline != null)
            {
                await _cancellationTokenSource.SafeCancelAsync();
                await _narratePipeline.UnloadAsync();
                _narratePipeline.Dispose();
                _narratePipeline = null;
                _currentPipeline = null;
            }

            IsLoaded = false;
            IsLoading = false;
            IsExecuting = false;
        }
    }


    public interface INarrateService
    {
        PipelineModel Pipeline { get; }
        bool IsLoaded { get; }
        bool IsLoading { get; }
        bool IsExecuting { get; }
        bool CanCancel { get; }
        Task LoadAsync(PipelineModel pipeline);
        Task UnloadAsync();
        Task CancelAsync();
        Task<AudioTensor> ExecuteAsync(NarrateRequest options);
    }


    public record NarrateRequest
    {
        public string InputText { get; set; }
        public string VoiceStyle { get; set; }
        public int Steps { get; set; } = 5;
        public float Speed { get; set; } = 1f;
        public float SilenceDuration { get; set; } = 0.3f;
        public int Seed { get; set; }
    }

}
