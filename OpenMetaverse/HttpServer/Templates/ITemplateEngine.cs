using System;
using System.Collections.Generic;

namespace HttpServer.Templates
{
    public interface ITemplateEngine
    {
        /// <summary>
        /// Render the template
        /// </summary>
        /// <param name="fileName">Filename of the template to render</param>
        /// <param name="variables">A list of key/value pairs to pass to the template</param>
        /// <returns>The rendered output</returns>
        /// <exception cref="FileNotFoundException">If template is not found.</exception>
        string Render(string fileName, IDictionary<string, object> variables);
    }
}
