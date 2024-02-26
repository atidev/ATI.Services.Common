namespace ATI.Services.Common.Behaviors;

public static class ApiMessages
{
    public const string InternalServerError = "Произошла ошибка во время выполнения запроса.";
    public const string TimeoutCommonMessage = "Сервер не ответил во время.";
    public const string ForbiddenCommonMessage = "Отсутствует разрешение.";
    public const string BadRequestCommonMessage = "Неверные входные данные.";
    public const string ExternalServiceError = "Произошла ошибка при получении данных";
    public const string ForbiddenOnlyPaidMessage = "Метод доступен только платным пользователям.";
    public const string DuplicatesErrorCommonMessage = "Объект дублирует существующий в базе.";
    public const string LogicalErrorCommonMessage = "Запрос противоречит условиям бизнес логики.";
    public const string ModificationRestrictedCommonMessage = "Объект защищен от удаления и редактирования.";
    public const string UnknownErrorMessage = "Произошла неизвестная ошибка";
    public const string NotFoundErrorMessage = "Ресурс не найден";
    public const string Unauthorized = "Не авторизован";
    public const string TooManyRequests = "Слишком много запросов.";
}