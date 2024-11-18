using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.IO;
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;

public class EmailTemplateFactory
{
    private readonly IRazorViewEngine _razorViewEngine;
    private readonly IServiceProvider _serviceProvider;

    public EmailTemplateFactory(IRazorViewEngine razorViewEngine, IServiceProvider serviceProvider)
    {
        _razorViewEngine = razorViewEngine;
        _serviceProvider = serviceProvider;
    }

    // Select the appropriate Razor view for rendering
    public IView GetEmailTemplate(string templateName)
    {
        var viewResult = _razorViewEngine.GetView(executingFilePath: null, viewPath: $"Views/EmailTemplates/{templateName}.cshtml", isMainPage: false);

        if (!viewResult.Success)
        {
            throw new InvalidOperationException("Could not find the email template.");
        }

        return viewResult.View;
    }

    // Render Razor view to string with a model
    public string RenderRazorViewToString<TModel>(string viewName, TModel model)
    {
        var view = GetEmailTemplate(viewName);  // Retrieve the view template

        using (var stringWriter = new StringWriter())
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());

            var viewDataDictionary = new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model
            };

            var viewContext = new ViewContext(
                actionContext,
                view,
                viewDataDictionary,
                new TempDataDictionary(actionContext.HttpContext, _serviceProvider.GetService<ITempDataProvider>()),
                stringWriter,
                new HtmlHelperOptions()
            );

            // Render the view to the string writer
            view.RenderAsync(viewContext).GetAwaiter().GetResult();

            return stringWriter.ToString();
        }
    }
}
