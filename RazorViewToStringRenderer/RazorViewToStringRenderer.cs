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

		/// <summary>
		/// Use it to override the DefaultHttpContext used
		/// </summary>
		public HttpContext HttpContext;
		/// <summary>
		/// Use it to specify specific RouteData
		/// </summary>
		public RouteData RouteData;
		/// <summary>
		/// Use it to specify specific ActionDescriptor
		/// </summary>
		public ActionDescriptor ActionDescriptor;

		/// <summary>
		/// Constructor for dependencies injection
		/// </summary>
		/// <param name="razorViewEngine"></param>
		/// <param name="tempDataProvider"></param>
		/// <param name="serviceProvider"></param>
		public RazorViewToStringRenderer(IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider,
			IServiceProvider serviceProvider)
		{
			this._razorViewEngine = razorViewEngine;
			this._tempDataProvider = tempDataProvider;
			this._serviceProvider = serviceProvider;
		}

		/// <summary>
		/// Convert Razor view to string
		/// </summary>
		/// <typeparam name="TModel">Type of model to pass to the Razor view</typeparam>
		/// <param name="viewName">Path or name of view</param>
		/// <param name="model">Model to pass to the Razor view</param>
		/// <returns>Razor view to string representation async</returns>
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

		/// <summary>
		/// Find Razor view in project
		/// </summary>
		/// <param name="actionContext">Context of action</param>
		/// <param name="viewName">Path or view of Razor view</param>
		/// <returns></returns>
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

		/// <summary>
		/// Return context of action
		/// </summary>
		/// <returns></returns>
		private ActionContext GetActionContext()
		{
			var httpContext = this.HttpContext ?? new DefaultHttpContext { RequestServices = this._serviceProvider};
			return new ActionContext(httpContext, this.RouteData ?? new RouteData(), this.ActionDescriptor ?? new ActionDescriptor());
		}
	}
}
