using System.Collections.Generic;
using System.Web.Mvc;

namespace SRSAD.Class
{
    public class SuccessJsonResult : JsonResult
    {
        public SuccessJsonResult(object data)
        {
            Data = new { success = true, data = data };
            JsonRequestBehavior = JsonRequestBehavior.AllowGet;
        }
    }

    public class ErrorJsonResult : JsonResult
    {
        public ErrorJsonResult(List<string> messages)
        {
            Data = new { success = false, messages = messages };
            JsonRequestBehavior = JsonRequestBehavior.AllowGet;
        }
    }

    public class JsonResultError : JsonResult
    {
        public JsonResultError(object data)
        {
            Data = new { success = true, response = "error", data = data };
            JsonRequestBehavior = JsonRequestBehavior.AllowGet;
        }
    }

    public class JsonResultSuccess : JsonResult
    {
        public JsonResultSuccess(object data)
        {
            Data = new { success = true, response = "success", data = data };
            JsonRequestBehavior = JsonRequestBehavior.AllowGet;
        }
    }

}