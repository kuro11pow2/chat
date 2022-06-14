﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interface
{
    public interface IClient
    {
        Task<IMessage> Receive();
        Task Send(IMessage message);
        Task Connect();
        void Disconnect();
        public bool IsConnected();
        public string GetCid();
        public string GetInfo();
    }
}
