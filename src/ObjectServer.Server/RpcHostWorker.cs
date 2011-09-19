﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace ObjectServer.Server
{
    public sealed class RpcHostWorker : AbstractWorker
    {
        private Thread workerThread = null;

        public RpcHostWorker()
            : base()
        {

            if (!Environment.Initialized)
            {
                throw new InvalidOperationException("无法应用启动服务器，请先初始化框架");
            }

            if (Environment.Configuration.RpcHandlerMax <= 0)
            {
                throw new IndexOutOfRangeException("无效的工人数量");
            }

            this.RpcHandlerMax = Environment.Configuration.RpcHandlerMax;
            this.RpcHandlerUrl = Environment.Configuration.RpcHandlerUrl;
            this.RpcHostUrl = Environment.Configuration.RpcHostUrl;
        }

        public int RpcHandlerMax { get; private set; }
        public string RpcHandlerUrl { get; private set; }
        public string RpcHostUrl { get; private set; }

        protected override void OnStart()
        {
            this.workerThread = new Thread(new ThreadStart(this.WorkerProc));
            this.workerThread.Start();
        }

        protected override void OnAbort()
        {
            Debug.Assert(this.workerThread != null);

            this.workerThread.Abort();
            this.workerThread = null;
        }

        private void WorkerProc()
        {
            var workersUrl = this.RpcHandlerUrl;
            var hostUrl = this.RpcHostUrl;

            LoggerProvider.EnvironmentLogger.Info(() => string.Format(
                "Starting all RPC-Handler threads: RPC-Entrance URL=[{0}]，RPC-Hander URL=[{1}]",
                hostUrl, workersUrl));

            using (var pool = new ZMQ.ZMQDevice.WorkerPool(
                hostUrl, workersUrl, RpcHandler.ProcessingLoop, (short)this.RpcHandlerMax))
            {
                this.WaitToStop(pool);
            }
        }

        private void WaitToStop(ZMQ.ZMQDevice.WorkerPool pool)
        {
            while (true)
            {
                var cmd = base.ReceiveControlCommand();
                if (cmd == "STOP" && pool.IsRunning)
                {

                    LoggerProvider.EnvironmentLogger.Info(
                        "'STOP' command received, try to stop the WorkerPool...");
                    StopWorkerPool(pool);
                    break;
                }
            }
        }

        private static void StopWorkerPool(ZMQ.ZMQDevice.WorkerPool pool)
        {
            Debug.Assert(pool != null);
            Debug.Assert(pool.IsRunning);

            pool.Stop();

            //等待 WorkerPool 停止
            while (pool.IsRunning)
            {
                Thread.Sleep(20);
            }

            Debug.Assert(!pool.IsRunning);
        }

    }
}
