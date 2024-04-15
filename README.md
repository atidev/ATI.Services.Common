# ATI.Services.Common
## Деплой


### Теги

Выкладка в nuget происходит на основе триггера на тег определённого формата: [как повышать версию](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
##### ВАЖНО: 
1. Все теги должны начинаться с `v`
2. Для тестовой версии тег должен быть с постфиксом, например `v1.0.1-rc`
3. Релизный тег должен состоять только из цифр версии, например `v1.0.0`

* Создание тега через git(нужно запушить его в origin) [создание тега и пуш в remote](https://git-scm.com/book/en/v2/Git-Basics-Tagging)
  * Команды для тестового:
    1. `git checkout <название ветки>`
    2. `git tag -a <название тега> -m "<описание тега>" ` 
    3. `git push --tags`
  * Команды для релизного:
    1. `git tag -a <название тега> <SHA коммита> -m "<описание тега>" `
    2. `git push --tags`
* Через раздел [releses](https://github.com/atidev/ATI.Services.Common/releases)(альфа версии нужно помечать соответсвующей галкой).
* При пуше, в некоторых IDE, необходимо отметить чекбокс об отправке тегов


#### Разработка теперь выглядит вот так:
1. Создаем ветку, пушим изменения, создаем pull request.
2. Добавляем на ветку тег с версией изменения
3. Срабатывает workflow билдит и пушит версию(берёт из названия тега) в nuget.
4. По готовности мерджим ветку в master.
5. Тегаем нужный коммит мастера.
Нужно обязательно описать изменения внесённые этим релизом в release notes
Здесь лучше воспользоваться интерфейсом гитхаба, там удобнее редактировать текст.
6. Срабатывает релизный workflow билдит и пушит в нугет релизную версию.
7. В разделе [Releses](https://github.com/atidev/ATI.Services.Common/releases) появляется информация о нашем релиз и release notes.

---
## Документация
### Redis

Подлкючения к redis и их настройка хранятся в `appsettings.json`
Для настройки redis в `Startup.cs` нужно добавить '`services.AddRedis()`
Для его корректной работы в `appsettings.json` необходимо добавить секцию `CacheManagerOptions`.
Пример:
```json
 "CacheManagerOptions": {
    "HitRatioManagerUpdatePeriod": "00:05:00",
    "CacheOptions": {
      "FirstCache": {
        "TimeToLive": "00:05:00",
        "ConnectionString": "your.firstredis.connectionstring",
        "RedisTimeout": "00:00:02",
        "CircuitBreakerSeconds": "00:01:00",
        "CircuitBreakerExceptionsCount": 20,
        "CacheDbNumber": 0
      },
      "SecondCache": {
        "TimeToLive": "01:00:00",
        "ConnectionString": "your.secondredis.connectionstring",
        "RedisTimeout": "00:00:02",
        "CircuitBreakerSeconds": "00:01:00",
        "CircuitBreakerExceptionsCount": 20,
        "CacheDbNumber": 0
      }
    }
  }
```
> поле HitRatioManagerUpdatePeriod нужно для работы механизма сбора метрик по проценту попадания записей в кэш, но этот функционал временно не работает.
 В случае такого конфига будут созданы два экземпляра c разными строками подключения и разным временем жизни.

Можно указать весь `ConnectionString`, заовверайдить его параметры через отдельные поля (`Servers` и тд), либо просто проставить только отдельные поля. Посмотреть их можно в `RedisOptions.cs`

В дальнейшнем, чтобы использовать redis, нужно через DI получить экземпляр `RedisProvider` и вызвать его метод `GetCache` (пример):
```c#
  public FirmCommentRepository(RedisProvider redisProvider)
  {
    IRedisCache _cache = redisProvider.GetCache(CacheNames.FirmComment.ToString());
  }
```

---
### Sql
Схема такая же как при работе с Redis.
Для настройки в `Startup.cs` нужно добавить '`services.AddSql()`

 Дополнительно, можно указать кастомный таймаут к определенной процедуре из базы:
```json
  "DbManagerOptions": {
    "DataBaseOptions": {
      "FirstDb": {
        "ConnectionString": "FirstDbConnectionString",
        "Timeout": "00:00:02",
        "TimeoutDictionary": {
          "Very_Heavy_Procedure_Name": "5"
         }
        }
        }
    }
```
 Можно указать весь `ConnectionString`, заовверайдить его параметры через отдельные поля (`Server`, `Database` и тд), либо просто проставить только отдельные поля. Посмотреть их можно в `DataBaseOptions.cs`
 В этом случае процедура на процедуру `Very_Heavy_Procedure_Name` будет установлен таймаут в 5 секунд, а на все остальные 2.

 Пример использования:
```c#
  public FirmCommentRepository(DbProvider provider)
  {
    IDbWrapper _db = provider.GetDb(Databases.RateInfo.ToString());
  }
```

---
### Метрики

#### Виды метрик
```
common_metric_sql - collected by DapperDb and PostgressDapper
common_metric_http_client - for outgoing http requests, ConsulMetricsHttpClientWrapper uses it
common_metric_rabbitmq_in - incoming messages from rmq, used by ATI.Services.RabbitMQ and ChangeTracking
common_metric_rabbitmq_out - outgoing messages to rmq, used by ATI.Services.RabbitMQ and ChangeTracking
common_metric_repository - should be collected manually
common_metric_controller - incoming http requests, added by MeasureAttribute in controllers
common_metric_Exceptions - application exceptions
common_metric_HttpStatusCodeCounter - aspnet response codes
common_metric_redis - collected by RedisCache
common_metric_mongo - should be collected manually
common_metric_{something} - this one reserved for custom metric, if you really need it, try to keep number of unique metrics as low as possible
```
#### Добавление в проект 

Так как Prometheus собирает метрики через консул, добавляем тег в конфиг консула `metrics-port-*портприложения*`.

```csharp
services.AddMetrics(); //или services.AddCommonMetrics();
//...
app.UseEndpoints(endpoints =>
    {
        //...
        endpoints.MapMetricsCollection(); //Добавляем эндпоинт для сбора метрик
        //...
    });

app.UseMetrics(); //Добавляем мидлвару
```

Для использования кастомных метрик в `appsettings.json` нужно определить следующую модель:
```json
"MetricsOptions": {
    "LabelsAndHeaders": {
      "Лейбл метрики" : "Header HTTP-запроса"
    },
  },
```
Ключ словаря - лейбл метрики, значение - Header HTTP-запроса.

Собственно сбор:
На метод котроллера вешаем `MeasureAttribute`, в который передаем название сущности, с которой работает метод.
В остальных файлах создаем нужный экземпляр `MetricsFactory` оборачиваем методы в using c `CreateMetricsTimer`:
```csharp
 private readonly MetricsFactory _metricsFactory = MetricsFactory.CreateRepositoryMetricsFactory(RepositoryName));
  using (_metricsFactory.CreateMetricsTimer(EntityName))
            {
              Entity entity = await DoSomething();
            }
```

---
### Http

Для удобства походов в другие сервисы через consul были написаны следующие классы:
1. `BaseServiceOptions`

Если вы хотите написать адаптер для похода в чужой сервис, нужно:
1. Завести класс `XServiceOptions`, отнаследовать его от `BaseServiceOptions`
2. Завести в `appsettings.json` секцию `XServiceOptions`, описать/переопределить все необходимые параметры. Указать `UseHttpClientFactory: "true"`
3. Зарегистрируйте его в `startup.cs` - `services.ConfigureByName<XServiceOptions>`
4. Добавьте в `startup.cs` `services.AddCustomHttpClient<XServiceOptions>`. Это добавит в `HttpClientFactory` HttpClient под именем настройки `ServiceName`
5. Напишите свой адаптер, пример использования:
```csharp
public class FirmsAdapter
{
        private readonly FirmServiceOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;

        public FirmsAdapter(IOptions<FirmServiceOptions> options, IHttpClientFactory httpClientFactory)
        {
            _options = options.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<OperationResult<FirmInfoForSettings>> GetFirmInfoAsync(int userId)
        {
            const string urlTemplate = "_internal/accounts/{0}";
        
            var url = string.Format(urlTemplate, userId);

            var httpClient = _httpClientFactory.CreateClient(_serviceOptions.ServiceName);

            return await httpClient.SendAsync<List<FirmInfoForSettings>>(HttpMethod.Get, url, MetricEntities.FirmService,
                urlTemplate: urlTemplate);
        }
}
```

`services.AddCustomHttpClient<XServiceOptions>()` делает следующее:
1. Добавляет `AdditionalHeaders` из `XServiceOptions` в каждый запрос
2. Добавляет `HttpLoggingHandler`, который логирует Exception/не 200 ответ от стороннего сервиса
3. Добавляет `HttpProxyFieldsHandler`, который проксирует поля `HeadersToProxy` из `XServiceOptions` в каждом запросе (вытаскивает их из `IHttpContextAccessor`)
4. Добавляет `Retry+CircuitBreaker+Timeout Policies (Handlers)`, параметры переопределения лежат в `BaseServiceOptions.cs`
5. Добавляет `HttpMetricsHandler`, который фиксирует время выполнения каждого запроса (каждого ретрая, если включена политика ретраев)

#### RetryPolicies
```csharp
{
    /// <summary>
    /// Timeout for one request. If you use RetryPolicy - it will be also a timeout for one request (not total time of policy)
    /// </summary>
    public TimeSpan TimeOut { get; set; }
    
    /// <summary>
    /// Set 0 if you dont want to use RetryPolicy
    /// </summary>
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// Delay between retries
    /// Median for spreading queries over time
    /// </summary>
    public TimeSpan MedianFirstRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Number of exceptions after which CB will be opened (will stop making requests)
    /// Set 0 if you dont want to use CB
    /// </summary>
    public int CircuitBreakerExceptionsCount { get; set; } = 20;
    
    /// <summary>
    /// Time after which CB will be closed (will make requests)
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Http methods to retry
    /// If not set - retry only GET methods
    /// </summary>
    public List<string> HttpMethodsToRetry { get; set; }
}
```
Если вы вызовете `AddCustomHttpClient<>`, по умолчанию будут включены все Policies. Если вы хотите выключить RetryPolicy - поставьте значение 0 у параметра `RetryCount`, CB Policy - `CircuitBreakerExceptionsCount`.
Также можно переопределить политики выполнения конкретного запроса. Для этого в `HttpClient.SendAsync` нужно передать настройку `retryPolicySettings`. NOTE - если передать не NULL, тогда проверки на `HttpMethodsToRetry` не будет (но на `RetryCount` - останется)

---
### Local Cache
Для работы с данными, которые меняются очень редко, сделан механизм кэширования в памяти сервиса. Готовим так:
наследуем класс, в котором будут хранится данные от класса  `LocalCache<T>`, где `T` - объект, который нужно хранить.
Добавляем в `Startup.cs` `services.AddLocalCache()`.

---
### Инициализатор
Необходимо проинициализировать все зависимости (consul, authorization, changetracker и т.д., в зависимости от того, что добавляли через `services.Add...()`).
Добавляем в `Startup.cs` `services.AddInitializers()`. Он автоматически найдет в сборке всех наследников от IInitializers и в порядке их поля Order (по возрастанию) проинициализирует их (порядок важен, к примеру, консул должен стартовать последним).
Для запуска в `Program.cs`:
```c#
 using (var scope = webHost.Services.CreateScope())
 {
    var serviceProvider = scope.ServiceProvider;
    await serviceProvider.GetRequiredService<StartupInitializer>().InitializeAsync();
  }
```
Можно добавлять свои кастомные инициализаторы, для этого наследуемся от `IInitializer` и задаем ему порядок инициализации через атрибут `[InitializeOrder(Order = {ваш порядок})]`

---
### Operation Results
Класс `OperationResult` используется во всех сервисах для соблюдения внутренних контрактов работы с базой и сторонними сервисами.
> Схема похожа на http статус-коды, но немного проще.

---
### Common results behavior
При общении с внешними сервисами для унификации и соблюдения контракта выдачи сделан класс `CommonBehavior`, все данные в контроллерах обрабатываются и приводятся к `ActionResult` только через него.
При необходимости можно заменить стандартный для наших сервисов сериализатор, вызвав метод этого класса `SetSerializer` и передать туда свой.
> Нужно отдавать себе отчет, что класс `CommonBehavior` статический, и это действие затронет весь сервис.


---
### Experimental results behavior
В проекте есть класс `ActionBuilder`, который выполняет тот же функционал, что и `CommonBehavior`, но в другом стиле и с более гибкими настойками.
Сейчас испрользуетcя в сервисе [иконок](http://stash.ri.domain:7990/projects/AS/repos/ati.iconservice/browse/ATI.IconService/Controllers/IconController.cs)
> Был введен недавно, и временем не проверен. Надо быть с ним осторожнее.

---
### Swagger
Для его использования в `appsettings.json` нужно определить следующую модель:
```json
 "SwaggerOptions": {
    "Enabled": true,
    "ServiceName": "ServiceName",
    "Version": "v1.1", //необязательно, по умолчанию v1.0
    "ProjectsXmlNames" : ["Project-1.xml", "Project-2.xml"], //необязательно, если не указать - возьмет эти названия с папки с билдом
    "SecurityApiKeyHeaders" : ["header-1","header-2"] //авторизационные хэдеры приложения
  }
```
Контроллеры наследуем от `ControllerWithOpenApi`. <br/>
На методы контроллера вешаем нужные SwaggerTag теги в атрибуте `[SwaggerTag(SwaggerTag.Internal|SwaggerTag.Public)]` и
виды ответов через `[ProducesResponseType(typeof(T), 200)]`.

В `Startup.cs` добавляем: <br/>
 `services.AddAtiSwagger();` <br/>
 `app.UseAtiSwagger();` <br/>
 Можно передать кастомные настройки через `Action`

 ---
### Кастомные атрибуты
Эти атрибуты нужны, так как при любом коде ошибки мы должны вернуть тело с описанием ошибок ([ссылка](http://gitlab.ri.domain/ati-api/standards/-/blob/rmq/docs/http-status-codes.md)).
1. `AtiUserRequiredAttribute`. Проверяет на наличие `X-Authenticated-User-Id` и возвращает 403 с телом ошибки при его отсутствии.
1. `ValidateModelStateAttribute`. Проверяет `ModelState` и возвращает 400 с телом ошибки

 ---
### slack
Схема такая же, как при работе с Redis.
Для настройки в `Startup.cs` нужно добавить `services.AddSlack()`

Добавляем такую секцию в` appsettings.json`
```json
  "SlackProviderOptions": {
    "SlackOptions": {
      "FailedChecks": {
        "AlarmChannel": "#driverchecks-alerts-staging",
        "BotName": "Watcher",
        "Emoji": ":dicaprio:",
        "SlackAddress": "https://hooks.slack.com",
        "WebHookUri": "url",
        "AlertsEnabled": true
      },
      "KonturFullCheck": {
        "AlarmChannel": "#kontur_full_check_request-staging",
        "BotName": "Watcher",
        "Emoji": ":dicaprio:",
        "SlackAddress": "https://hooks.slack.com",
        "WebHookUri": "url",
        "AlertsEnabled": true
      }
    }
  }
```

 Пример использования:
```c#
  public KonturFullCheckAlertSender(SlackProvider slackProvider)
  {
    _slackAdapter = slackProvider.GetAdapter(SlackChannel.KonturFullCheck.ToString());
  }
```

---
### Логи
Для использования NLog нужно:
1. Создать секцию NLogOptions в `appsettings`
2. Подключить NLog `.UseNLog()` в Program.cs
3. Настроить NLog после инициализации ConfigurationManager в Startup.cs
```
var nLogOptions = ConfigurationManager.ConfigurationRoot.GetSection("NLogOptions").Get<NLogOptions>();
var nLogConfigurator = new NLogConfigurator(nLogOptions);
nLogConfigurator.ConfigureNLog();
```

Структура секции NLogOptions в appsettiings, default значения можно опустить:
``` json
"NLogOptions": {
    "ThrowExceptions": false, //default
    "AddGeneralAttributes": true, //default | Использовать ли общие атрибуты, список можно посмотреть в NLogConfigurator.cs:35
    "Variables": [
        {
            "Name": "applicationName",
            "Value": "debug"
        }
    ],
    "Attributes": [
        {
            "Name": "custom",
            "Layout": "custom",
            "EscapeUnicode": false, //default
            "EncodeJson": true, //default
            "IncludeEmptyValue": false //default
        }
    ],
    "Rules": [
        {
            "TargetName": "jsonFile",
            "MinLevel": "Warn", //default
            "MaxLevel": "Off", //default | Off значит без верхнего ограничения
            "LoggerNamePattern": "*" //default
        }
    ],
    "FileTargets": [
        {
            "Name": "jsonFile",
            "FileName": "${basedir}/Log/NLog.Errors.json", //default
            "MaxArchiveFiles": 7, //default
            "ArchiveNumbering": "Date", //default
            "ArchiveEvery": "Day", //default
            "ArchiveDateFormat": "yyyyMMdd", //default
            "ArchiveFileName": "${basedir}/Log/NLog.Error.{##}.json", //default
            "AddGeneralAttributes": true, //default | аналогично AddGeneralAttributes в корне, но влияет только на этот таргет
            "Attributes": [
                {
                    // Специфичные для таргета атрибуты
                }
            ]
        }
    ],
    "NetworkTargets": [
        {
            "Name": "logStash",
            "address": "udp4://network-target.address",
            "keepConnection": true, //default
            "onOverflow": "Split", //default
            "newline": true, //default
            "Attributes": [
                {
                    // Специфичные для таргета атрибуты
                }
            ]
        }
    ],
    "LoggedRequestHeader" : ["header-1","header-2"] //логируемые хэдеры входящих HTTP запросов
}
```
---
### ServiceVariables
Данный блок конфигурации используется для конфигурирования данных уровня всего приложения
В приложении можно вызывать
```csharp
ServiceVariables.Variables
```
Так же имеются предопределенные поля:
```csharp
ServiceVariables.ServiceAsClientHeaderName
ServiceVariables.ServiceAsClientName
ServiceVariables.DefaultLocale
ServiceVariables.SupportedLocales
```
Структура секции ServiceVariables
``` json
  "ServiceVariablesOptions": {
    "Variables": {
      //Передается в каждый исходящий HTTP Запрос ConsulMetricsHttpClientWrapper в качестве header'a со значением  ServiceAsClientName
      "ServiceAsClientHeaderName": "ClientNameHeader",
      "ServiceAsClientName": "ServiceName", //имя сервиса при исходящих HTTP запросах
      "DefaultLocale":"ru", //локаль, использующаяся по умолчанию
      "VarName-3":"Var value 3", //Дополнительные параметры
      "VarName-4":"Var value 4" //Дополнительные параметры
    },
    "SupportedLocales":["ru","en"] //список поддерживаемых сервисом локалей
  }
```
В `Startup.cs` вызываем
```c#
  services.AddServiceVariables();
```

### Localization

Поддержана работа по локализации, для работы нужно добавить `DefaultLocale` и `SupportedLocales` в `ServiceVariablesOptions`
и использовать в Startup (в самом начале)
```csharp
app.UseAcceptLanguageLocalization();
```
Теперь, если заголовок Accept-Language при http запросе или хэдер accept_language rmq сообщения будет передан, то получить значение культуры можно в любом месте приложения путем вызова
```csharp
LocaleHelper.GetLocale();
```
Значение хэдеров, без парсинга, хранится в FlowContext и его можно получить
```csharp
FlowContext<RequestMetaData>.AccessLanguage;
```

#### InCodeLocalizer

Небольшой хелпер для локализации строк, переводы можно хранить прямо в коде. Подходит если строк для перевода относительно мало.
Работает только с локалями перечисленными в `ServiceVariables.SupportedLocales`, текущую локаль определяет через `LocaleHelper.GetLocale()`

1. Добавить в Startup.cs
```csharp
services.AddInCodeLocalization();
```

2. Реализовать `IInCodeLocalization`, есть 2 варианта:
   1. для всех локалей кроме дефолтной
     в качестве ключа использовать значение в дефолтной локали,
    более наглядно, т.к. в коде по месту используются не "NortWestRegionId", а сам текст в дефолтной локали
      ```csharp
      public class EnLocalization : IInCodeLocalization
      {
          public string Locale { get; } = new("en");

          public ReadOnlyDictionary<string, string> LocalizedStrings =>
              new(new Dictionary<string, string>
              {
                  { "Северо-Западный фед.округ", "Northwestern Federal District" },
              });
      }

      // использование
      // в дефолтной локали вернёт переданный ключ "Северо-Западный фед.округ"
      _inCodeLocalizer["Северо-Западный фед.округ"]
      ```
   2. для каждой поддерживаемой локали
      ```csharp

      public class RuLocalization : IInCodeLocalization
      {
          public string Locale { get; } = new("ru");

          public ReadOnlyDictionary<string, string> LocalizedStrings =>
              new(new Dictionary<string, string>
              {
                  { "NortWestRegionId", "Северо-Западный фед.округ" },
              });
      }

      public class EnLocalization : IInCodeLocalization
      {
          public string Locale { get; } = new("en");

          public ReadOnlyDictionary<string, string> LocalizedStrings =>
              new(new Dictionary<string, string>
              {
                  { "NortWestRegionId", "Northwestern Federal District" },
              });
      }

      // использование
      _inCodeLocalizer["NortWestRegionId", false]
      ```

### Xss
Небольшой сервис для проверки на xss инъекции

#### Middleware
Добавить в Startup.cs

```csharp
app.UseXssValidation();
```

#### Атрибуты

##### Атрибут для контроллера
Над котроллером написать
```csharp
[XssInputValidationFilter]
```
##### Атрибут для свойств в классах
  1. Добавить в Startup.cs
  ```csharp
  services.AddXssValidationAttribute();
  ```
  2. Над свойствами в классе добавить
  ```csharp
  [XssValidate]
  ```

#### Аттрибут XssSanitizer
Атрибут который экранирует Xss, либо выдает ошибку на моменте валидации модели. Атрибут добавляется к полям.
Чтобы валидации прошла, на метод, в котором требуется валидации модели с аттрибутом `XssSanitizer` нужно добавить аттрибут
```csharp
[ValidateModelState]
```
По умолчанию атрибут будет эранировать xss. Для того чтобы была ошибка на моменте валидации, нужно передать параметр в атрибут следующим образом
```csharp
[XssSanitizer(IsReplace = false)]
```



