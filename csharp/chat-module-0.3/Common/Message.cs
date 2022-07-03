using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    using Interface;
    public enum MessageType
    {
        MESSAGE,

        REQ_START = 100,
        PING,
        BROADCAST,
        REQ_END,

        RES_START = 200,
        BAD_REQ,
        CANT_HANDLE_REQ,
        SUCCESS,
        FAILURE,
        RES_END,
    }
    public class Message
    {
        public MessageType Type { get; set; } = MessageType.MESSAGE;
        public string Str { get; set; } = "";

        public void SetMessage(string str)
        {
            Type = MessageType.MESSAGE;
            Str = str;
        }
        public void SetPing()
        {
            Type = MessageType.PING;
        }

        public void SetBroadcast(string str)
        {
            Type = MessageType.BROADCAST;
            Str = str;
        }
        public void SetBadReq()
        {
            Type = MessageType.BAD_REQ;
        }
        public void SetCantHandleReq()
        {
            Type = MessageType.CANT_HANDLE_REQ;
        }
        public void SetSuccess()
        {
            Type = MessageType.SUCCESS;
        }
        public void SetFailure()
        {
            Type = MessageType.FAILURE;
        }

        public string GetInfo()
        {
            return $"{nameof(MessageType)}: {Type}\n{nameof(Str)}: {Str}";
        }
    }
}
