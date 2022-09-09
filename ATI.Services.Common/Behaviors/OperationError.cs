﻿using System;
using Newtonsoft.Json;

namespace ATI.Services.Common.Behaviors
{
    [Serializable]
    public class OperationError
    {
        /// <summary>
        /// Причина ошибки (reason)
        /// </summary>
        public string ErrorMessage { get; }
        
        /// <summary>
        /// Ошибка (error)
        /// </summary>
        public string Error { get; }
        public ActionStatus ActionStatus { get; set; }

        [JsonIgnore]
        public bool IsInternal { get; set; }
        /// <summary>
        /// Создает экземпляр класса с сообщением об ошибке, переданным в параметре <paramref name="errorMessage"/>.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="errorMessage"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public OperationError(ActionStatus status, string errorMessage, bool isInternal = true)
        {
            ActionStatus = status;
            ErrorMessage = errorMessage;
            IsInternal = isInternal;
        }
        
        public OperationError(ActionStatus status, string errorMessage, string error, bool isInternal = true)
        {
            ActionStatus = status;
            Error = error;
            ErrorMessage = errorMessage;
            IsInternal = isInternal;
        }

        public override string ToString()
        {
            return ErrorMessage;
        }
    }
}
