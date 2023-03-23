using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using RoslynAnalysis.Core;

namespace RoslynAnalysis
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> instance to resolve application services.
        /// </summary>
        public IServiceProvider Services { get; }

        /// <summary>
        /// Gets the current <see cref="App"/> instance in use
        /// </summary>
        public new static App Current => (App)Application.Current;

        public App()
        {
            Services = ConfigureServices();

            this.InitializeComponent();

            //注册全局事件
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            foreach (var typeInfo in RuntimeHelper.GetAllTypes().Where(t => t.GetCustomAttribute(typeof(ServiceDescriptorAttribute)) != null))
            {
                var serviceDescriptorAttr = typeInfo.GetCustomAttribute(typeof(ServiceDescriptorAttribute)) as ServiceDescriptorAttribute;
                services.Add(new ServiceDescriptor(serviceDescriptorAttr.ServiceType, typeInfo, serviceDescriptorAttr.Lifetime));
            }

            return services.BuildServiceProvider();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            const string msg = "主线程异常";
            try
            {
                if (args.ExceptionObject is Exception && Dispatcher != null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        Exception ex = (Exception)args.ExceptionObject;
                        HandleException(msg, ex);
                    });
                }
            }
            catch (Exception ex)
            {
                HandleException(msg, ex);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            const string msg = "子线程异常";
            try
            {
                HandleException(msg, args.Exception);
                args.Handled = true;
            }
            catch (Exception ex)
            {
                HandleException(msg, ex);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs args)
        {
            const string msg = "异步异常";
            try
            {
                HandleException(msg, args.Exception);
                args.SetObserved();
            }
            catch (Exception ex)
            {
                HandleException(msg, ex);
            }
        }

        private static void HandleException(string msg, Exception ex)
        {
            var lines = new List<string>();
            Exception innerEx = ex;
            while (innerEx != null)
            {
                lines.Add(innerEx.Message);
                innerEx = innerEx.InnerException;
            }

            string detail = lines.ExpandAndToString("\r\n---");
            MessageBox.Show($"错误消息：{ex}", "错误提示", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}