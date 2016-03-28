﻿#region Licence
/*
    Babbacombe SockLib
    https://github.com/trevorprinn/SockLib
    Copyright © 2015 Babbacombe Computers Ltd.

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
    USA
 */
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Babbacombe.SockLib {
    /// <summary>
    /// A UDP server that can be used to advertise the IP address and port number of a SockLib server on a network.
    /// </summary>
    /// <remarks>
    /// DiscoverClient broadcasts UDP packets which this server reponds to.
    /// </remarks>
    public class DiscoverServer : IDisposable {
        private UdpClient _server;

        /// <summary>
        /// The data that gets sent when a client request is received.
        /// </summary>
        private byte[] _advertisment;

        private string _serviceName;

        private CancellationTokenSource _cancel;

        private Task _runTask;

        /// <summary>
        /// Starts the discovery server on the given port, advertising a service
        /// on the same port.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="port"></param>
        public DiscoverServer(int port, string serviceName) : this(port, serviceName, port) { }

        /// <summary>
        /// Starts the discovery server on the given port, advertising a service
        /// on a different port.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="serviceName"></param>
        /// <param name="servicePort"></param>
        public DiscoverServer(int port, string serviceName, int servicePort) {
            _serviceName = serviceName;
            _advertisment = Encoding.UTF8.GetBytes(string.Format("{0}:{1}", serviceName, servicePort));

            _server = new UdpClient(port);
			_server.EnableBroadcast = true;
            _cancel = new CancellationTokenSource();

			_runTask = Task.Run(() => runServer(_cancel.Token), _cancel.Token);
        }

		private async Task runServer(CancellationToken cancel) {
			while (!cancel.IsCancellationRequested) {
				var msg = await _server.ReceiveAsync(cancel);
				if (!cancel.IsCancellationRequested && msg != null &&
					Encoding.UTF8.GetString(msg.Value.Buffer) == _serviceName) {
					await _server.SendAsync(_advertisment, msg.Value.RemoteEndPoint, cancel);
				}
			}
			System.Diagnostics.Debug.WriteLine("Server stopped");
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				_cancel.Cancel();
				_runTask.Wait();
				_server.Close();
				_cancel.Dispose();
			}
		}

        public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
        }
    }
}
