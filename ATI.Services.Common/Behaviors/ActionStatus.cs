namespace ATI.Services.Common.Behaviors
{
    /// <summary>
    /// Специфический статус действия с объектом. В случае успешного действия, ActionStatus = None.
    /// </summary>
    public enum ActionStatus
    {
        /// <summary>Операция успешно выполнена</summary>
        Ok = 0,
        /// <summary>Запрашиваемый объект отсутствует.</summary>
        NoContent = 1,
        /// <summary>У пользователя отсутствует разрешение на запрашиваемое действие с объектом.</summary>
        Forbidden = 2,
        /// <summary>Внутренняя ошибка.</summary>
        InternalServerError = 3,
        /// <summary>
        /// Данные не найдены
        /// </summary>
        NotFound = 4,
        /// <summary>
        /// Неверные входные данные
        /// </summary>
        BadRequest = 5,
        /// <summary>
        /// Не прошло ограничения БД
        /// </summary>
        ConstraintError = 6,
        /// <summary>
        /// Timeout
        /// </summary>
        Timeout = 7,
        /// <summary>
        /// Запрещено редактирование/удаление сущности из соображений бизнес логики.
        /// </summary>
        ModificationRestricted = 8,
        /// <summary>
        /// Дубликат уже существующей в базе сущности.
        /// </summary>
        Duplicates = 9,
        /// <summary>
        /// Дубликат уже существующей в базе у данной фирмы сущности.
        /// </summary>
        SelfDuplicates = 10,
        /// <summary>
        /// Операция запрещена из соображений бизнес логики.
        /// </summary>
        LogicalError = 11,
        /// <summary>
        /// Операция не выполнена, так как вышел из строя сервис, без которого можно работать
        /// </summary>
        InternalOptionalServerUnavailable = 12,
        /// <summary>
        /// Ошибка внешнего поставщика данных
        /// </summary>
        ExternalServerError = 13,
        /// <summary>
        /// Ошибка контракта данных с внешним поставщиком
        /// </summary>
        ExternalContractError = 14,
        /// <summary>
        /// Имеется какая-то логическая ошибка, из-за которой невозможно произвести операцию над ресурсом
        /// </summary>
        UnprocessableEntity = 15,
        /// <summary>
        /// Недостаточно денег
        /// </summary>
        PaymentRequired = 16,
        /// <summary>
        /// Ошибка конфигурации
        /// </summary>
        ConfigurationError = 17,
        /// <summary>
        /// Не авторизован
        /// </summary>
        Unauthorized = 18,
        /// <summary>
        /// Необходима полная регистрация
        /// </summary>
        FullRegistrationRequired = 19,
        /// <summary>
        /// Превышено допустимая частота запросов
        /// </summary>
        TooManyRequests = 20
    }
}
