# ATI.Services.Common
## Деплой
Выкладка в nuget происходит на основе триггера на тег определённого формата 
- `v1.0.0` - формат релизная версия на ветке master 
- `v1.0.0-rc1` - формат тестовой/альфа/бета версии на любой ветке  

Тег можно создать через git(нужно запушить его в origin) [создание тега и пуш в remote](https://git-scm.com/book/en/v2/Git-Basics-Tagging)  
или через раздел [releses](https://github.com/atidev/ATI.Services.Common/releases)(альфа версии нужно помечать соответсвующей галкой).

#### Разработка теперь выглядит вот так:
1. Создаем ветку, пушим изменения, создаем pull request.
2. Вешаем на ветку тег например `v1.0.2-new-auth-12`
3. Срабатывает workflow билдит и пушит версию(берёт из названия тега) в nuget.
4. По готовности мерджим ветку в master.
5. Вешаем релизный тег на нужный коммит мастера.
Нужно обязательно описать изменения внесённые этим релизом в release notes
Здесь лучше воспользоваться интерфейсом гитхаба, там удобнее редакитровать текст.
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
 В этом случае процедура на процедуру `Very_Heavy_Procedure_Name` будет установлен таймаут в 5 секунд, а на все остальные 2.
 
 Пример использования:
```c#
  public FirmCommentRepository(DbProvider provider)
  {
    IDbWrapper _db = provider.GetDb(Databases.RateInfo.ToString());
  } 
```

---
### Tracing
*внимание! Трейсинг работает ТОЛЬКО вкупе с системой метрик, потому необходимо выполнить инициализацию метрик.
добавляем в файл сеттингов 
```json
 "TracingOptions": {
    "Enabled": true,
    "Rate": 0,
    "TraceEndpoint": "http://zip-test-1.ri.domain:9411/",
    "ServiceName": "ATI.Notifications",
    "MetricsServiceName": "notifications" //переопределяем название сервиса для метрик
  }
```
В `Startup.cs` в методе `ConfigureServices(IServiceCollection services)` вызывваем 
```csharp
 services.AddTracing(ConfigurationManager.ConfigurationRoot.GetSection("TracingOptions"));
```
и в `Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)`
вызываем 
```csharp
 app.UseTracing();
```
Конфигурим `TracingOptions`, добавляем в IoC-контейнер `ZipkinManager`.
Инициализируем трейсинг, вызвав следующий код `Program.cs`:
```csharp
 var tracingOptions = serviceProvider.GetRequiredService<IOptions<TracingOptions>>().Value;
                var zipkinManager = serviceProvider.GetRequiredService<ZipkinManager>();
                zipkinManager.Init(tracingOptions);
```

---
### Метрики
Добавляем метрики в `Startup.cs` : `services.AddMetrics();`
Он автоматически добавит: `TracingOptions, ZipkinManager`

Так как Prometheus собирает метрики через консул, добавляем тег в конфиг консула `metrics-port-*портприложения*`.
Добавляем [endpoint](http://stash.ri.domain:7990/projects/AS/repos/ati.firmservicecore/browse/ATI.FirmService.Core.Web.Api/Controllers/MetricsController.cs) для сбора метрик.

Добавляем мидлвару 
```csharp
    app.UseMetrics();
```

Для использования кастомных метрик в `appsettings.json` нужно определить следующую модель:
```json
"MetricsOptions": {
    "LabelsAndHeaders": {
      "Лейбл метрики" : "Header HTTP-запроса"
    }
  },
```
Ключ словаря - лейбл метрики, значение - Header HTTP-запроса.



Просим девопсов добавить сервис в Prometheus.

Собственно сбор:
На метод котроллера вешаем `MeasureAttribute`, в который передаем название сущности, с которой работает метод.
В остальных файлах создаем нужный экземпляр `MetricsTracingFactory` оборачиваем методы в using c `CreateMetricsTimer`:
```csharp
 private readonly MetricsTracingFactory _metricsFactory = MetricsTracingFactory.CreateRepositoryMetricsFactory(RepositoryName));
  using (_metricsFactory.CreateMetricsTimer(EntityName))
            {
              Entity entity = await DoSomething();
            }
```
HTTP запросы к другим сервисам нужно оборачивать с трейсингом, например:
```csharp
 HttpClient _httpClient = CreateSomeHttpClient();
 MetricsTracingFactory _metricsTracingFactory = MetricsTracingFactory.CreateTracingFactory(httpServiceName);
 Func<Dictionary<string, string>> GetTracingInfo(string url, string method, string body) =>
            TraceHelper.GetHttpTracingInfo(url, method, body);
    using (_metricsTracingFactory.CreateTracingTimer(GetTracingInfo(requestUri, HttpMethod.Post.Method, body)))
            {
                return await _httpClient.PostAsync(requestUri, body);
            }
```
Для удобства был написан `ConsulMetricsHttpClientWrapper`. <br/>
Включает в себя `ConsulServiceAddress`, `TracingHttpClientWrapper` и `MetricsTracingFactory`. <br/>
Для инициализации нужно передать настройки сервиса, отнаследовав их от `BaseServiceOptions`, <br/>
`adapterName` (будет отображаться в метриках), <br/>
`serializer` (необязательный параметр, по умолчанию - `SnakeCase`) <br/>
Пример использования:
```csharp
public class FirmsAdapter
    {
        private readonly ConsulMetricsHttpClientWrapper _httpWrapper;

        private const string GetAccountUrlFormat = "_internal/accounts/{0}";
        private const string GetAccountsUrl = "_internal/accounts";

        public FirmsAdapter(IOptions<FirmServiceOptions> serviceOptions)
        {
            _httpWrapper = new ConsulMetricsHttpClientWrapper(serviceOptions.Value, nameof(FirmsAdapter));
        }

        public async Task<OperationResult<FirmInfoForSettings>> GetFirmInfoAsync(int firmId)
        {
            var request = new HttpRequestParams(GetAccountUrlFormat,
                string.Format(GetAccountUrlFormat, firmId),
                MetricEntity.FirmAccount,
                new {FirmId = firmId});

            return await _httpWrapper.GetAsync<FirmInfoForSettings>(request);
        }

        public async Task<OperationResult<List<FirmInfoForSettings>>> GetFirmsInfoAsync(List<int> firmIds)
        {
            var request = new HttpRequestParamsWithBody<List<int>>(GetAccountsUrl,
                GetAccountsUrl,
                MetricEntity.FirmAccount,
                firmIds,
                new {FirmIds = firmIds});

            return await _httpWrapper.PostAsync<List<int>, List<FirmInfoForSettings>>(request);
        }
```

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
### ServiceVaribles
Данный блок конфигурации используется для конфигурирования данных уровня всего приложения
В приложении можно вызывать 
```csharp
ServiceVaribles.Variables 
```
Так же имеются предопределенные поля:
```csharp
ServiceVaribles.ServiceAsClientHeaderName 
ServiceVaribles.ServiceAsClientName 
```
Структура секции ServiceVaribles
``` json
  "ServiceVariablesOptions": {
    "Variables": {
      //Передается в каждый исходящий HTTP Запрос ConsulMetricsHttpClientWrapper в качестве header'a со значением  ServiceAsClientName
      "ServiceAsClientHeaderName": "ClientNameHeader", 
      "ServiceAsClientName": "ServiceName", //имя сервиса при исходящих HTTP запросах
      "VarName-3":"Var value 3", //Дополнительные параметры
      "VarName-4":"Var value 4" //Дополнительные параметры
    }
  }
```
В `Startup.cs` вызываем 
```c#
  services.AddServiceVariables();
```
