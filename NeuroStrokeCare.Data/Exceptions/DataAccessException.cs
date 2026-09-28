using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeuroStrokeCare.Data.Exceptions
{
    public class DataAccessException : Exception
    {
        public DataAccessException(Exception EX, string CustomeMessage, ILogger Logger) : base(CustomeMessage, EX)
        {
            Logger.LogError($"Exception Error {EX.Message} Developer Comment {CustomeMessage}");
        }
    }
}
