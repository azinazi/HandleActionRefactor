using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Mvc;
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

        public HandleResultBuilder(T inputModel, IInvoker invoker)
        {
            _inputModel = inputModel;
            _invoker = invoker;
        }

        public HandleResultBuilder<T, TRet> Returning<TRet>()
        {
            return new HandleResultBuilder<T, TRet>(_inputModel, _invoker);
        }

        public static implicit operator HandleResult(HandleResultBuilder<T> builder)
        {
            return new HandleResult(builder);
        }
        public class HandleResult : ActionResult
        {
            private readonly HandleResultBuilder<T> _handlBuilder;

            public HandleResult(HandleResultBuilder<T> builder)
            {
                _handlBuilder = builder;
            }
            //public void Returning
            public override void ExecuteResult(ControllerContext context)
            {

            }

        }
    }
    

    public class HandleResultBuilder<T, TRet>
    {
        private readonly T _inputModel;
        private readonly IInvoker _invoker;
        private Func<TRet, ActionResult> _success;
        private Func<TRet, ActionResult> _error;
        private readonly List<ReturnActions<TRet>> _returnActions;
        private TRet _result;

        public HandleResultBuilder(T inputModel, IInvoker invoker)
        {
            _inputModel = inputModel;
            _invoker = invoker;
            _returnActions = new List<ReturnActions<TRet>>();
        }

        public HandleResultBuilder<T, TRet> On(Func<TRet, bool> condition, Func<TRet, ActionResult> actionCallback)
        {
            _returnActions.Add(new ReturnActions<TRet>(condition, actionCallback));
            return this;
        }


        public HandleResultBuilder<T, TRet> OnSuccess(Func<TRet, ActionResult> successCallback)
        {
            _success = successCallback;
            return this;
        }

        public HandleResultBuilder<T, TRet> OnError(Func<TRet, ActionResult> errorCallback)
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
                _builder._result = _builder._invoker.Execute<TRet>(_builder._inputModel);

                if (!context.Controller.ViewData.ModelState.IsValid && _builder._error != null)
                {
                    _builder._error(_builder._result).ExecuteResult(context);
                }
                else
                {
                   var returnAction =  _builder._returnActions.FirstOrDefault(x => x.Condition(_builder._result));

                   if (returnAction != null)
                    {
                        returnAction.Action(_builder._result).ExecuteResult(context);
                        return;
                    }

                    if (_builder._success != null)
                        _builder._success(_builder._result).ExecuteResult(context);
                }
            }
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