using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Course_service.Filter
{
    public class CheckFileFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {

            //only accept file csv and excel
            var file = context.HttpContext.Request.Form.Files[0];
            var fileExtension = Path.GetExtension(file.FileName);
            if (fileExtension != ".csv" && fileExtension != ".xlsx")
            {
                context.Result = new BadRequestObjectResult("File extension is not supported");
            }

            //check file size
            if (file.Length > 1048576)
            {
                context.Result = new BadRequestObjectResult("File size is too large");
            }
            //check file is empty
            if (file.Length == 0)
            {
                context.Result = new BadRequestObjectResult("File is empty");
            }
            // check file is null
            if (file == null)
            {
                context.Result = new BadRequestObjectResult("File is null");
            }
            //accept 1 file
            if (context.HttpContext.Request.Form.Files.Count > 1)
            {
                context.Result = new BadRequestObjectResult("Only accept 1 file");
            }
            base.OnActionExecuting(context);
        }
    }
}
