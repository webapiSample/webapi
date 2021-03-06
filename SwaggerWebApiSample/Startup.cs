﻿

[assembly: Microsoft.Owin.OwinStartup(typeof(SwaggerWebApiSample.Startup))]

namespace SwaggerWebApiSample
{
    using global::Owin;
    using Microsoft.Web.Http.Routing;
    //using Swashbuckle.Application;
    using Swagger.Net.Application;
    using System.IO;
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Http.Description;
    using System.Web.Http.Routing;

    /// <summary>
    /// Represents the startup process for the application.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Configures the application using the provided builder.
        /// </summary>
        /// <param name="builder">The current application builder.</param>
        public void Configuration(IAppBuilder builder)
        {
            // we only need to change the default constraint resolver for services that want urls with versioning like: ~/v{version}/{controller}
            var constraintResolver = new DefaultInlineConstraintResolver() { ConstraintMap = { ["apiVersion"] = typeof(ApiVersionRouteConstraint) } };
            var configuration = new HttpConfiguration();
            var httpServer = new HttpServer(configuration);
            //configuration.SuppressDefaultHostAuthentication

            // reporting api versions will return the headers "api-supported-versions" and "api-deprecated-versions"
            configuration.AddApiVersioning(o => o.ReportApiVersions = true);
            configuration.MapHttpAttributeRoutes(constraintResolver);

            // add the versioned IApiExplorer and capture the strongly-typed implementation (e.g. VersionedApiExplorer vs IApiExplorer)
            // note: the specified format code will format the version as "'v'major[.minor][-status]"
            var apiExplorer = configuration.AddVersionedApiExplorer(o => o.GroupNameFormat = "'V'VVV");
            var thisAssembly = typeof(Startup).Assembly;
            configuration.EnableSwagger(
                            "{apiVersion}/swagger",
                            swagger =>
                            {
                                // build a swagger document and endpoint for each discovered API version
                                swagger.MultipleApiVersions(
                                    (apiDescription, version) => apiDescription.GetGroupName() == version,
                                    info =>
                                    {
                                        foreach (var group in apiExplorer.ApiDescriptions)
                                        {
                                            var description = "一个api实例项目使用Swagger, Swashbuckle, Microsoft.AspNet.WebApi.Versioning.";

                                            if (group.IsDeprecated)
                                            {
                                                description += " <br/><b>此 API 版本已弃用！</b>";
                                            }

                                            info.Version(group.Name, $"版本 {group.ApiVersion}")
                                                .Contact(c => c.Name("Shijun Liu"))
                                                .Description(description)
                                                .License(l => l.Name("MIT").Url("https://opensource.org/licenses/MIT"))
                                                .TermsOfService("Shareware");
                                        }
                                    });

                                // add a custom operation filter which sets default values
                                swagger.OperationFilter<SwaggerDefaultValues>();
                                //swagger.BasicAuth("basic")
                                //.Description("Basic HTTP Authentication");

                                swagger.IncludeAllXmlComments(typeof(Startup).GetTypeInfo().Assembly, System.AppDomain.CurrentDomain.RelativeSearchPath);
                            })
                         .EnableSwaggerUi(swagger =>
                         {
                             //swaggerUi.api.clientAuthorizations.add("key", new SwaggerClient.ApiKeyAuthorization("Authorization", "Basic dXNlcm5hbWU6cGFzc3dvcmQ=", "header"));
                             //swagger.InjectJavaScript
                             swagger.EnableDiscoveryUrlSelector();                             
                             swagger.InjectJavaScript(thisAssembly, "SwaggerWebApiSample.CustomContent.api-key-header-auth.js");
                         });

            builder.UseWebApi(httpServer);
        }
    }
}