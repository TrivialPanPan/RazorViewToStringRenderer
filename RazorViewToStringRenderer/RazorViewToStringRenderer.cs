using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace RazorViewToStringRenderer
{
	public class RazorViewToStringRenderer : IRazorViewToStringRenderer
	{
		private readonly IRazorViewEngine _razorViewEngine;
		private readonly ITempDataProvider _tempDataProvider;
		private readonly IServiceProvider _serviceProvider;

		public RazorViewToStringRenderer(IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider,
			IServiceProvider serviceProvider)
		{
			this._razorViewEngine = razorViewEngine;
			this._tempDataProvider = tempDataProvider;
			this._serviceProvider = serviceProvider;
		}

		public async Task<string> RenderViewToStringRenderAsync<TModel>(string viewName, TModel model)
		{
			try
			{
				var actionContext = GetActionContext();
				var view = FindView(actionContext, viewName);

				using (var output = new StringWriter())
				{
					var viewContext = new ViewContext(actionContext, view,
						new ViewDataDictionary<TModel>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
						{
							Model = model
						},
						new TempDataDictionary(actionContext.HttpContext, _tempDataProvider), output, new HtmlHelperOptions());

					await view.RenderAsync(viewContext);

					return output.ToString();
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException($"The model '{model}' have some issues : {ex.Message}",
					ex.InnerException);
			}
		}

		private IView FindView(ActionContext actionContext, string viewName)
		{
			var getResultView = this._razorViewEngine.GetView(null, viewName, true);

			if (getResultView.Success)
			{
				return getResultView.View;
			}

			var findViewResult = this._razorViewEngine.FindView(actionContext, viewName, true);

			if (findViewResult.Success)
			{
				return findViewResult.View;
			}

			var searchedLocations = getResultView.SearchedLocations.Concat(findViewResult.SearchedLocations);
			var errorMessage = string.Join(Environment.NewLine, new[]
					{$"Unable to find view '{viewName}'. The following locations were searched : "}
				.Concat(searchedLocations));
			throw new InvalidOperationException(errorMessage);
		}

		private ActionContext GetActionContext()
		{
			var httpContext = new DefaultHttpContext { RequestServices = this._serviceProvider};
			return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
		}
	}
}
