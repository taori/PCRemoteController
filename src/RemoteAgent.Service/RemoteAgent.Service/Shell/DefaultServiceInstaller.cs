using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace RemoteAgent.Service.Shell
{
	[RunInstaller(true)]
	internal class DefaultServiceInstaller : Installer
	{
		private ServiceProcessInstaller _process;
		private ServiceInstaller _service;

		public DefaultServiceInstaller()
		{
			_process = new ServiceProcessInstaller();
			_process.Account = ServiceAccount.LocalSystem;
			_service = new ServiceInstaller();
			_service.StartType = ServiceStartMode.Automatic;

			_service.ServiceName = ConfigurationManager.AppSettings["serviceName"];
			_service.DisplayName = ConfigurationManager.AppSettings["serviceDisplayName"];
			_service.Description = ConfigurationManager.AppSettings["serviceDescription"];

			Installers.Add(_process);
			Installers.Add(_service);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._process?.Dispose();
				this._service?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}