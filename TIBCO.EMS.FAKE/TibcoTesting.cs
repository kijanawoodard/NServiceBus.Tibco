using System.Collections.Generic;

namespace TIBCO.EMS
{
	public static class TibcoTesting
	{
		private static Dictionary<string, IMessageListener> _destinations = new Dictionary<string, IMessageListener>();

		public static void RegisterListener(string name, IMessageListener listener)
		{
			_destinations[name] = listener;
		}

		public static void SendMessage(string name, string data)
		{
			var message = new TextMessage(new Session(), data);
			_destinations[name].OnMessage(message);
		}
	}
}