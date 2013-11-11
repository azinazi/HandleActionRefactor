using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.Routing;
using SchoStack.Web;
using System.Linq;

namespace HandleActionRefactor.Controllers
{
    public class BaseController : Controller
    {
        public IInvoker Invoker { get; set; }

        protected HandleResultBuilder<T> Handle<T>(T inputModel)
        {
            return new HandleResultBuilder<T>(inputModel, Invoker);
        }
    }

    public class HandleResultBuilder<T>
    {
        private readonly T _inputModel;
        private readonly IInvoker _invoker;
        private Func<ActionResult>  _error;
        private Func<ActionResult> _success;

        public HandleResultBuilder(T inputModel, IInvoker invoker)
        {
            _inputModel = inputModel;
            _invoker = invoker;
        }

        public HandleResultBuilder<T> OnError(Func<ActionResult> errorCallback)
        {
            
            _error = errorCallback;
            return this;
        }

        public HandleResultBuilder<T> OnSuccess(Func<ActionResult> successCallback)
        {
            
            _success = successCallback;
            return this;
        }

        public HandleResultBuilder<T, TRet> Returning<TRet>()
        {
            return new HandleResultBuilder<T, TRet>(_inputModel, _invoker,_error,_success);
        }

        public static implicit operator HandleResult(HandleResultBuilder<T> builder)
        {
            return new HandleResult(builder);
        }
        public class HandleResult : ActionResult
        {
            private readonly HandleResultBuilder<T> _builder;

            public HandleResult(HandleResultBuilder<T> builder)
            {
                _builder = builder;
            }
            //public void Returning
            public override void ExecuteResult(ControllerContext context)
            {
                if (!context.Controller.ViewData.ModelState.IsValid && _builder._error != null)
                {
                    _builder._error().ExecuteResult(context);
                }
                else
                {
                   _builder._invoker.Execute(_builder._inputModel);                  

                    if (_builder._success != null)
                        _builder._success().ExecuteResult(context);
                }
            }

        }
    }


    public class HandleResultBuilder<T, TRet>
    {
        private readonly T _inputModel;
        private readonly IInvoker _invoker;
        private Func<TRet,ControllerContext, ActionResult> _success;
        private Func<ActionResult> _error;
        private readonly List<ReturnActions<TRet>> _returnActions;

        public HandleResultBuilder(T inputModel, IInvoker invoker, Func<ActionResult> error, Func<ActionResult> success)
        {
            _inputModel = inputModel;
            _invoker = invoker;
            _returnActions = new List<ReturnActions<TRet>>();
            _error = error;
            _success =(_,__ )=> success();
        }

        public HandleResultBuilder<T, TRet> On(Func<TRet, bool> condition, Func<TRet, ActionResult> actionCallback)
        {
            _returnActions.Add(new ReturnActions<TRet>(condition, actionCallback));
            return this;
        }

        public HandleResultBuilder<T, TRet> OnSuccess(Func<TRet,ControllerContext, ActionResult> successCallback)
        {
            _success = successCallback;
            return this;
        }

        public HandleResultBuilder<T, TRet> OnSuccess(Func<TRet, ActionResult> successCallback)
        {
            _success = (x,_) => successCallback(x);
            return this;
        }

        public HandleResultBuilder<T, TRet> OnError(Func<ActionResult> errorCallback)
        {
            _error = errorCallback;
            return this;
        }

        public static implicit operator HandleResult(HandleResultBuilder<T, TRet> builder)
        {
            return new HandleResult(builder);
        }

        public class HandleResult : ActionResult
        {
            private readonly HandleResultBuilder<T, TRet> _builder;

            public HandleResult(HandleResultBuilder<T, TRet> builder)
            {
                _builder = builder;
            }

            public override void ExecuteResult(ControllerContext context)
            {
            

                if (!context.Controller.ViewData.ModelState.IsValid && _builder._error != null)
                {
                    _builder._error().ExecuteResult(context);
                }
                else
                {    
                    var result = _builder._invoker.Execute<TRet>(_builder._inputModel);
                    var returnAction = _builder._returnActions.FirstOrDefault(x => x.Condition(result));

                    if (returnAction != null)
                    {
                        returnAction.Action(result).ExecuteResult(context);
                        return;
                    }

                    if (_builder._success != null)
                        _builder._success(result,context).ExecuteResult(context);
                }
            }
        }
    }

    public static class HandleCustomActions
    {

        public static HandleResultBuilder<T, TRet> OnSuccessWithMessage<T, TRet>(this HandleResultBuilder<T, TRet> handleResultBuilder, 
            Func<TRet, ActionResult> redirectTo, string message)
        {
            
            return handleResultBuilder.OnSuccess((returnModel,controllerContext)=>
            {
                controllerContext.Controller.TempData.Add("Message", message);
                return redirectTo(returnModel);
            }
                );
        }
    }

    public class ReturnActions<TRet>
    {
        public ReturnActions(Func<TRet, bool> condition, Func<TRet, ActionResult> action)
        {
            Condition = condition;
            Action = action;
        }

        public Func<TRet, bool> Condition { get; set; }
        public Func<TRet, ActionResult> Action { get; set; }
    }
}