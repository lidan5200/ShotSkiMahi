using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ShotSkiMahiD.Services;
using ShotSkiMahiD.ViewModels;
using ShotSkiMahiD.Views;

namespace ShotSkiMahiD
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        private MainWindow? _mainWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            
            _serviceProvider = serviceCollection.BuildServiceProvider();
            
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            _mainWindow.Show();

            LaunchMesConfigTool();
        }

        private static void LaunchMesConfigTool()
        {
            try
            {
                var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var toolPath = Path.Combine(baseDirectory, "cfg", "MES2.0参数配置工具_V1.0.0.1.20160820.exe");
                
                if (File.Exists(toolPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = toolPath,
                        UseShellExecute = true,
                        WorkingDirectory = Path.Combine(baseDirectory, "cfg")
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动MES配置工具失败: {ex.Message}");
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var settingIniPath = Path.Combine(baseDirectory, "ShotSkiConfig", "setting.ini");
            var mesConfigIniPath = Path.Combine(baseDirectory, "cfg", "mes_config.ini");
            var logDirectory = Path.Combine(baseDirectory, "Logs");
            
            var logService = new LogService(logDirectory, 7);
            var configService = new ConfigService(settingIniPath, mesConfigIniPath, logService);
            var systemConfig = configService.LoadSystemConfig();
            var mesConfig = configService.LoadMesConfig();
            
            services.AddSingleton(configService);
            services.AddSingleton(systemConfig);
            services.AddSingleton(mesConfig);
            services.AddSingleton(logService);

            services.AddSingleton<ProductionStatsService>(sp =>
            {
                var logService = sp.GetRequiredService<LogService>();
                return new ProductionStatsService(logDirectory, logService);
            });
            
            services.AddSingleton<PlcService>(sp => 
                new PlcService(systemConfig.Plc1Config, systemConfig.Plc2Config));
            
            services.AddSingleton<ScanService>(sp => 
                new ScanService(systemConfig.ScanConfig));
            
            services.AddSingleton<MesApiService>(sp => 
            {
                var service = new MesApiService(mesConfig, systemConfig.ApiConfig, systemConfig);
                var logService = sp.GetRequiredService<LogService>();
                service.SetLogService(logService);
                var configService = sp.GetRequiredService<ConfigService>();
                service.SubscribeConfigChanges(configService);
                return service;
            });
            
            services.AddSingleton<ProcessController>(sp => 
            {
                var plcService = sp.GetRequiredService<PlcService>();
                var scanService = sp.GetRequiredService<ScanService>();
                var mesApiService = sp.GetRequiredService<MesApiService>();
                var logService = sp.GetRequiredService<LogService>();
                
                return new ProcessController(
                    plcService,
                    scanService,
                    mesApiService,
                    logService,
                    systemConfig.ScanConfig,
                    systemConfig.MagnetConfig);
            });
            
            services.AddSingleton<MainViewModel>(sp =>
            {
                var plcService = sp.GetRequiredService<PlcService>();
                var scanService = sp.GetRequiredService<ScanService>();
                var mesApiService = sp.GetRequiredService<MesApiService>();
                var processController = sp.GetRequiredService<ProcessController>();
                var logService = sp.GetRequiredService<LogService>();
                var configService = sp.GetRequiredService<ConfigService>();
                var statsService = sp.GetRequiredService<ProductionStatsService>();

                return new MainViewModel(
                    plcService,
                    scanService,
                    mesApiService,
                    processController,
                    logService,
                    configService,
                    statsService);
            });
            services.AddSingleton<MainWindow>();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
        }
    }
}
