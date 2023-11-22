using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOSControl.Exceptions
{
    internal class UPOSException : Exception
    {
        public UPOSException(int errorCode, int errorCodeExtended = 0,
            string description = "", Exception origException = null) : base(description)
        {
            this.ErrorCode = errorCode;
            this.ErrorCodeExtended = errorCodeExtended;
            this.OrigException = origException;
        }

        public int GetErrorCode()
        {
            return ErrorCode;
        }

        public int GetErrorCodeExtended()
        {
            return ErrorCodeExtended;
        }

        public Exception GetOrigException()
        {
            return OrigException;
        }

        protected int ErrorCode;
        protected int ErrorCodeExtended;
        private Exception OrigException;

    }
}
