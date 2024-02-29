namespace ATI.Services.Common.Swagger
{
    public class SwaggerOptions
    {
        public bool Enabled { get; set; }
        
        /// <summary>
        /// Будет отображаться на странице сваггера
        /// </summary>
        public string ServiceName { get; set; }
        public string Version { get; set; } = "v1.0";
        
        /// <summary>
        /// Массив. Заполнять значениями {ProjectName}.xml
        /// Для начала нужно в .csproj, в секции PropertyGroup добавить элемент:
        /// &lt;GenerateDocumentationFile&gt;true&lt;/GenerateDocumentationFile&gt;
        /// Если отправить null, то попытаемся получить список *.xml файлов из папки сервиса (работает в большинстве случаев)
        /// </summary>
        public string[] ProjectsXmlNames{ get; set; }

        public string[] SecurityApiKeyHeaders { get; set; } = { };
    }
}