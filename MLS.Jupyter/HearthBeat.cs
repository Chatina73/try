﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NetMQ;
using NetMQ.Sockets;
using Pocket;
using static Pocket.Logger<MLS.Jupyter.Heartbeat>;

namespace MLS.Jupyter
{
    public class Heartbeat : IHostedService
    {
        private readonly string _address;
        private readonly ResponseSocket _server;

        public Heartbeat(ConnectionInformation connectionInformation)
        {
            if (connectionInformation == null)
            {
                throw new ArgumentNullException(nameof(connectionInformation));
            }

            _address = $"{connectionInformation.Transport}://{connectionInformation.IP}:{connectionInformation.HBPort}";

            Log.Info($"using address {nameof(_address)}", _address);
            _server = new ResponseSocket();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            _server.Bind(_address);

            using (Log.OnEnterAndExit())
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var data = _server.ReceiveFrameBytes();

                    // Echoing back whatever was received
                    _server.TrySendFrame(data);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server.Dispose();
            return Task.CompletedTask;
        }
    }
}