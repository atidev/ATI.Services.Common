﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using JetBrains.Annotations;

namespace ATI.Services.Common.Behaviors
{
    [PublicAPI]
    public class OperationResult
    {
        /// <summary>
        /// Возвращает или задает флаг, указывающий, успешно ли была выполнена операция.
        /// </summary>
        public bool Success => ActionStatus == ActionStatus.Ok;
        public IList<OperationError> Errors { get; } = new List<OperationError>();
        public Dictionary<string, object> Details { get; } = new();

        /// <summary>
        /// Статус действия с объектом.
        /// </summary>
        public ActionStatus ActionStatus { get; protected set; }        

        public OperationResult(ActionStatus actionStatus)
        {
            ActionStatus = actionStatus;
        }

        public OperationResult(HttpStatusCode httpStatusCode)
        {
            ActionStatus = GetActionStatusByHttpStatusCode(httpStatusCode);
        }

        public OperationResult(ActionStatus actionStatus, string errorMessage, bool isPrivate = true)
        {
            Errors.Add(new OperationError(actionStatus, errorMessage, isPrivate));
            ActionStatus = actionStatus;
        }
        
        /// <summary>
        /// Создает экземпляр класса и добавляет ошибку <param name="errorMessage"/> с кодом <param name="error"/> в коллекцию ошибок/> .
        /// </summary>
        /// <param name="actionStatus"></param>
        /// <param name="errorMessage"></param>
        /// <param name="error"></param>
        /// <param name="isPrivate"></param>
        public OperationResult(ActionStatus actionStatus, string errorMessage, string error, bool isPrivate = false)
        {
            Errors.Add(new OperationError(actionStatus, errorMessage, error, isPrivate));
            ActionStatus = actionStatus;
        }
        
        /// <summary>
        /// Создает экземпляр класса на основе ошибки <param name="operationError"/>.
        /// </summary>
        /// <param name="operationError"></param>
        public OperationResult(OperationError operationError)
        {
            Errors.Add(operationError);
            ActionStatus = operationError.ActionStatus;
        }
        
        public OperationResult(OperationError operationError, Dictionary<string, object> details)
        {
            Errors.Add(operationError);
            ActionStatus = operationError.ActionStatus;
            Details = details;
        }

        public OperationResult(ActionStatus actionStatus, List<OperationError> errors)
        {
            ActionStatus = actionStatus;
            Errors = errors;
        }

        public OperationResult(OperationResult operationResult)
        {
            ActionStatus = operationResult.ActionStatus;
            Errors = operationResult.Errors;
            Details = operationResult.Details;
            Exception = operationResult.Exception;
        }

        public OperationResult(Exception exception, string message = null)
        {
            ActionStatus = ActionStatus.InternalServerError;
            Errors.Add(new OperationError(ActionStatus, message ?? exception.Message));
            Exception = exception;
        }
        
        public Exception Exception { get; protected set; }
        
        /// <summary>
        /// Выводит все сообщения об ошибках, разделенных пустой строкой.
        /// </summary>
        /// <returns></returns>
        public string DumpAllErrors()
        {
            return Errors.Count > 0 ? string.Join(Environment.NewLine + Environment.NewLine, Errors) : string.Empty;
        }

        public string DumpPublicErrors()
        {
            return Errors.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine + Environment.NewLine, Errors.Where(error => !error.IsInternal));
        }

        internal static ActionStatus GetActionStatusByHttpStatusCode(HttpStatusCode httpStatusCode)
        {
            switch (httpStatusCode)
            {
                case var code when (int)code >= 200 && (int)code < 300:
                    return ActionStatus.Ok;
                case HttpStatusCode.BadRequest:
                    return ActionStatus.BadRequest;
                case HttpStatusCode.Unauthorized:
                    return ActionStatus.Unauthorized;
                case HttpStatusCode.PaymentRequired:
                    return ActionStatus.PaymentRequired;
                case HttpStatusCode.Forbidden:
                    return ActionStatus.Forbidden;
                case HttpStatusCode.NotFound:
                    return ActionStatus.NotFound;
                case HttpStatusCode.RequestTimeout:
                    return ActionStatus.Timeout;
                case HttpStatusCode.TooManyRequests:
                    return ActionStatus.TooManyRequests;
                case HttpStatusCode.NotModified:
                    return ActionStatus.NotModified;
                default:
                    return ActionStatus.InternalServerError;
            }
        }

        public static OperationResult Ok { get; } = new(ActionStatus.Ok);
    }

    /// <summary>
    /// Представляет результат операции вида TrySomething(), содержащий признак успешного завершения (<see cref="OperationResult.Success"/>) и значение <see cref="Value"/>.
    /// </summary>
    /// <typeparam name="TValue">Тип значения, возвращаемого операцией.</typeparam>
    [PublicAPI]
    public class OperationResult<TValue> : OperationResult
    {
        /// <summary>
        /// Возвращает флаг, указывающий, успешно ли была выполнена операция и проверяет что значение не равно null        
        /// </summary>
        [MemberNotNullWhen(returnValue: true, nameof(Value))]
        public new bool Success => ActionStatus == ActionStatus.Ok && Value != null &&
                                   (!UseCountSuccessCondition || !ValueIsArray || ((ICollection)Value).Count != 0);

        /// <summary>
        /// Возвращает или задает результат выполнения операции.
        /// </summary>
        public TValue Value { get; }

        private bool UseCountSuccessCondition { get; set; }

        private bool ValueIsArray => typeof(ICollection).IsAssignableFrom(typeof(TValue));

        /// <param name="value"></param>
        /// <param name="actionStatus"></param>
        /// <param name="useCountSuccessCondition">Success вернет false, если этот флаг установлен и <see cref="Value"/> .Count = 0</param>
        public OperationResult(TValue value, ActionStatus actionStatus = ActionStatus.Ok, bool useCountSuccessCondition = false) : base(actionStatus)
        {
            Value = value;
            UseCountSuccessCondition = useCountSuccessCondition;
        }

        /// <param name="actionStatus"></param>
        /// <param name="useCountSuccessCondition">Success вернет false, если этот флаг установлен и <see cref="Value"/> .Count = 0</param>
        public OperationResult(ActionStatus actionStatus = ActionStatus.Ok, bool useCountSuccessCondition = false) : base(actionStatus)
        {
            UseCountSuccessCondition = useCountSuccessCondition;
        }

        public OperationResult(HttpStatusCode httpStatusCode, bool useCountSuccessCondition = false) : base(httpStatusCode)
        {
            UseCountSuccessCondition = useCountSuccessCondition;
        }

        public OperationResult(OperationResult<TValue> operationResult, bool useCountSuccessCondition = false) : base(operationResult)
        {
            Value = operationResult.Value;
            UseCountSuccessCondition = useCountSuccessCondition;
        }

        public OperationResult(OperationResult operationResult, bool useCountSuccessCondition = false) : base(operationResult)
        {
            UseCountSuccessCondition = useCountSuccessCondition;
        }

        /// <summary>
        /// Создает экземпляр класса со значениями полей <see cref="OperationResult.Success"/> и <see cref="Value"/> , переданными в параметрах ,<param name="errorMessage"></param> и <paramref name="value"/> соответственно и добавляет ошибку <param name="errorMessage"/> в коллекцию ошибок/> .
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="value"></param>
        /// <param name="actionStatus"></param>
        /// <param name="useCountSuccessCondition">Success вернет false, если этот флаг установлен и <see cref="Value"/> .Count = 0</param>
        public OperationResult(string errorMessage, TValue value, ActionStatus actionStatus, bool useCountSuccessCondition = false, bool isPrivate = true) : base(actionStatus, errorMessage, isPrivate)
        {
            Value = value;
            UseCountSuccessCondition = useCountSuccessCondition;
        }
        
        /// <summary>
        /// Создает экземпляр класса со значениями полей <see cref="OperationResult.Success"/> , переданными в параметрах <param name="errorMessage"></param>  соответственно и добавляет ошибку <param name="errorMessage"/> в коллекцию ошибок/> .
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <param name="actionStatus"></param>
        public OperationResult(ActionStatus actionStatus, string errorMessage, bool isPrivate = true) 
            : base(actionStatus, errorMessage, isPrivate)
        {
        }

        /// <summary>
        /// Создает экземпляр класса и добавляет ошибку <param name="errorMessage"/> с кодом <param name="error"/> в коллекцию ошибок/> .
        /// </summary>
        /// <param name="actionStatus"></param>
        /// <param name="errorMessage"></param>
        /// <param name="error"></param>
        /// <param name="isPrivate"></param>
        public OperationResult(ActionStatus actionStatus, string errorMessage, string error, bool isPrivate = false) :
            base(actionStatus, errorMessage, error, isPrivate)
        {
        }

        /// <summary>
        /// Создает экземпляр класса на основе ошибки <param name="operationError"/>.
        /// </summary>
        /// <param name="operationError"></param>
        public OperationResult(OperationError operationError) : base(operationError)
        {
        }
        
        public OperationResult(OperationError operationError, Dictionary<string, object> details) : base(operationError, details)
        {
        }

        public OperationResult(Exception exception, string message = null) : base(exception)
        { }
    }
}