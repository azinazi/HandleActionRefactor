using System;
using System.Linq.Expressions;
using System.Web.Mvc;
using SchoStack.Web;

namespace HandleActionRefactor.Controllers
{
    public class BaseController : Controller
    {
        public IInvoker Invoker { get; set; }

        protected HandleResult<T> Handle<T>(T inputModel)
        {
            return new HandleResult<T>(inputModel,Invoker);
        }
    }

    public class HandleResult<T>:ActionResult
    {
        private readonly T _inputModel;
        private readonly IInvoker _invoker;

        public HandleResult(T inputModel, IInvoker invoker)
        {
            _inputModel = inputModel;
            _invoker = invoker;
        }

        public HandleResult<T, TRet> Returning<TRet>()
        {
            return new HandleResult<T, TRet>(_inputModel, _invoker);
        }

        //public void Returning
        public override void ExecuteResult(ControllerContext context)
        {
            
        }

    }

    public class HandleResult<T, TRet>:ActionResult
    {
        private readonly T _inputModel;
        private readonly IInvoker _invoker;
        private Func<T, ActionResult> _success;
        private Func<T, ActionResult> _error;
        private Func<T, ActionResult> _returnUrl;
        private TRet _result;

        public HandleResult(T inputModel, IInvoker invoker)
        {
            _inputModel = inputModel;
            _invoker = invoker;
            _result  = _invoker.Execute<TRet>(_inputModel);
        }

        public HandleResult<T, TRet> On(Func<TRet, bool> func , Func<T, ActionResult> func1)
        {
            //MemberExpression body = (MemberExpression)func.Invoke().;
           // string name = body.Member.Name;
            _returnUrl = func1;
            return this;
        }


        public HandleResult<T, TRet> OnSuccess(Func<T, ActionResult> func)
        {
            _success = func;
            return this;
        }

        public HandleResult<T, TRet> OnError(Func<T, ActionResult> func)
        {     
            _error = func;
            return this;
        }

        public override void ExecuteResult(ControllerContext context)
        {
          
//            if (_returnUrl != null)      
//                _returnUrl(_inputModel).ExecuteResult(context);


            if (_error != null)
                _error(_inputModel).ExecuteResult(context);

            if (_success != null)      
                _success(_inputModel).ExecuteResult(context);   

               
        }
    }
}