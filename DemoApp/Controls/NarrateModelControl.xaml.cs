using DemoApp.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using TensorStack.Common;
using TensorStack.WPF;
using TensorStack.WPF.Controls;

namespace DemoApp.Controls
{
    /// <summary>
    /// Interaction logic for NarrateModelControl.xaml
    /// </summary>
    public partial class NarrateModelControl : BaseControl
    {
        private ListCollectionView _modelCollectionView;
        private Device _selectedDevice;
        private NarrateModel _selectedModel;
        private Device _currentDevice;
        private NarrateModel _currentModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="NarrateModelControl"/> class.
        /// </summary>
        public NarrateModelControl()
        {
            LoadCommand = new AsyncRelayCommand(LoadAsync, CanLoad);
            UnloadCommand = new AsyncRelayCommand(UnloadAsync, CanUnload);
            InitializeComponent();
        }

        public static readonly DependencyProperty SettingsProperty = DependencyProperty.Register(nameof(Settings), typeof(Settings), typeof(NarrateModelControl), new PropertyMetadata<NarrateModelControl>((c) => c.OnSettingsChanged()));
        public static readonly DependencyProperty IsSelectionValidProperty = DependencyProperty.Register(nameof(IsSelectionValid), typeof(bool), typeof(NarrateModelControl));
        public static readonly DependencyProperty CurrentPipelineProperty = DependencyProperty.Register(nameof(CurrentPipeline), typeof(PipelineModel), typeof(NarrateModelControl), new PropertyMetadata<NarrateModelControl>((c) => c.OnPipelineChanged()));

        public event EventHandler<PipelineModel> SelectionChanged;
        public AsyncRelayCommand LoadCommand { get; }
        public AsyncRelayCommand UnloadCommand { get; }

        public Settings Settings
        {
            get { return (Settings)GetValue(SettingsProperty); }
            set { SetValue(SettingsProperty, value); }
        }

        public bool IsSelectionValid
        {
            get { return (bool)GetValue(IsSelectionValidProperty); }
            set { SetValue(IsSelectionValidProperty, value); }
        }

        public PipelineModel CurrentPipeline
        {
            get { return (PipelineModel)GetValue(CurrentPipelineProperty); }
            set { SetValue(CurrentPipelineProperty, value); }
        }

        public Device SelectedDevice
        {
            get { return _selectedDevice; }
            set { SetProperty(ref _selectedDevice, value); }
        }

        public NarrateModel SelectedModel
        {
            get { return _selectedModel; }
            set { SetProperty(ref _selectedModel, value); }
        }

        public ListCollectionView ModelCollectionView
        {
            get { return _modelCollectionView; }
            set { SetProperty(ref _modelCollectionView, value); }
        }

        private async Task LoadAsync()
        {
            if (!await IsModelValidAsync())
                return;

            _currentDevice = SelectedDevice;
            _currentModel = SelectedModel;
            CurrentPipeline = new PipelineModel
            {
                Device = _currentDevice,
                NarrateModel = _currentModel
            };
        }


        private bool CanLoad()
        {
            var isReloadRequired = SelectedDevice is not null
                && SelectedModel is not null
                && HasCurrentChanged();

            var isSelectionValid = !isReloadRequired;
            if (IsSelectionValid != isSelectionValid)
                IsSelectionValid = isSelectionValid;

            return isReloadRequired;
        }


        private Task UnloadAsync()
        {
            _currentModel = default;
            CurrentPipeline = new PipelineModel
            {
                Device = _selectedDevice
            };

            return Task.CompletedTask;
        }


        private bool CanUnload()
        {
            return _currentModel is not null;
        }


        private bool HasCurrentChanged()
        {
            return _currentDevice != SelectedDevice
                || _currentModel != SelectedModel;
        }


        private Task OnSettingsChanged()
        {
            ModelCollectionView = new ListCollectionView(Settings.NarrateModels);
            ModelCollectionView.Filter = (obj) =>
            {
                if (obj is not NarrateModel viewModel)
                    return false;

                if (_selectedDevice == null)
                    return false;

                return viewModel.SupportedDevices?.Contains(_selectedDevice.Type) ?? false;
            };

            SelectedDevice = Settings.DefaultDevice;
            return Task.CompletedTask;
        }


        private void Device_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ModelCollectionView?.Refresh();
            if (ModelCollectionView is not null)
            {
                SelectedModel = ModelCollectionView.Cast<NarrateModel>().FirstOrDefault(x => x == _currentModel || x.IsDefault);
            }
        }


        private Task OnPipelineChanged()
        {
            SelectedDevice = CurrentPipeline?.Device ?? Settings.DefaultDevice;
            if (CurrentPipeline?.NarrateModel is not null)
            {
                SelectedModel = CurrentPipeline.NarrateModel;
            }

            _currentDevice = CurrentPipeline?.Device;
            _currentModel = CurrentPipeline?.NarrateModel;
            SelectionChanged?.Invoke(this, CurrentPipeline);
            return Task.CompletedTask;
        }


        private async Task<bool> IsModelValidAsync()
        {
            if (_selectedModel is null)
                return false;

            if (_selectedModel.IsValid)
                return true;

            return await _selectedModel.DownloadAsync(Path.Combine(Settings.DirectoryModel, "Narrate"));
        }
    }
}
