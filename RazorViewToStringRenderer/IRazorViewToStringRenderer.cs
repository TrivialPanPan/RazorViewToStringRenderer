using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace RazorViewToStringRenderer
{
	public interface IRazorViewToStringRenderer
	{
		Task<string> RenderViewToStringRenderAsync<TModel>(string viewName, TModel model);
	}
}
