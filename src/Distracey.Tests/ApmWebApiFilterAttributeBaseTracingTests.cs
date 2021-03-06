﻿using System;
using System.Net.Http;
using System.Web.Http.Filters;
using Distracey.Web.WebApi;
using NUnit.Framework;

namespace Distracey.Tests
{
    [TestFixture]
    public class ApmWebApiFilterAttributeBaseTracingTests
    {
        private string _applicationName;
        private bool _addResponseHeaders;
        private Action<IApmContext, ApmWebApiStartInformation> _startAction;
        private Action<IApmContext, ApmWebApiFinishInformation> _finishAction;
        private TestApmWebApiFilterAttribute _testApmWebApiFilterAttribute;

        [SetUp]
        public void Setup()
        {
            _applicationName = "ApplicationName";
            _addResponseHeaders = true;
            _startAction = (context, information) => { };
            _finishAction = (context, information) => { };
            _testApmWebApiFilterAttribute = new TestApmWebApiFilterAttribute(_applicationName, _addResponseHeaders, _startAction, _finishAction);
        }

        [Test]
        public void WhenLoggingStartOfRequest()
        {
            var startActionLogged = false;
            var applicationName = string.Empty;
            var eventName = string.Empty;
            var flags = string.Empty;
            var methodsIdentifier = string.Empty;
            var parentSpanId = string.Empty;
            var sampled = string.Empty;
            var spanId = string.Empty;
            var traceId = string.Empty;
            var httpRequest = default(HttpRequestMessage);
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.TraceIdHeaderKey, "TestClient=1234");
            actionContext.Request.Headers.Add(Constants.SpanIdHeaderKey, "SpecialProcess=4321");
            actionContext.Request.Headers.Add(Constants.ParentSpanIdHeaderKey, "ParentSpecialProcess=5678");
            actionContext.Request.Headers.Add(Constants.SampledHeaderKey, "Sampled");
            actionContext.Request.Headers.Add(Constants.FlagsHeaderKey, "Flags");

            _startAction = (context, information) =>
            {
                startActionLogged = true;
                applicationName = information.ApplicationName;
                eventName = information.EventName;
                flags = information.Flags;
                methodsIdentifier = information.MethodIdentifier;
                parentSpanId = information.ParentSpanId;
                sampled = information.Sampled;
                spanId = information.SpanId;
                traceId = information.TraceId;
                httpRequest = information.Request;
            };

            _testApmWebApiFilterAttribute = new TestApmWebApiFilterAttribute(_applicationName, _addResponseHeaders, _startAction, _finishAction);
            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsNotNull(httpRequest);
            Assert.IsTrue(startActionLogged);
            Assert.IsNotEmpty(applicationName);
            Assert.IsNotEmpty(eventName);
            Assert.AreEqual("TestClient=1234", traceId);
            Assert.AreEqual("SpecialProcess=4321", spanId);
            Assert.AreEqual("ParentSpecialProcess=5678", parentSpanId);
            Assert.AreEqual("Sampled", sampled);
            Assert.AreEqual("Flags", flags);

            var controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
            var methodType = actionContext.Request.Method;
            var actionName = actionContext.ActionDescriptor.ActionName;
            var arguments = string.Empty;

            var expectedMethodIdentifier = string.Format("{0}.{1}({2}) - {3}", controllerName, actionName, arguments, methodType);

            Assert.AreEqual(expectedMethodIdentifier, methodsIdentifier);

        }

        [Test]
        public void WhenLoggingEndOfRequest()
        {
            var finishActionLogged = false;
            var applicationName = string.Empty;
            var eventName = string.Empty;
            var responseTime = 0L;
            var exception = default(object);
            var flags = string.Empty;
            var methodsIdentifier = string.Empty;
            var parentSpanId = string.Empty;
            var sampled = string.Empty;
            var spanId = string.Empty;
            var traceId = string.Empty;
            var httpRequest = default(HttpRequestMessage);
            var httpResponse = default(HttpResponseMessage);
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.TraceIdHeaderKey, "TestClient=1234");
            actionContext.Request.Headers.Add(Constants.SpanIdHeaderKey, "SpecialProcess=4321");
            actionContext.Request.Headers.Add(Constants.ParentSpanIdHeaderKey, "ParentSpecialProcess=5678");
            actionContext.Request.Headers.Add(Constants.SampledHeaderKey, "Sampled");
            actionContext.Request.Headers.Add(Constants.FlagsHeaderKey, "Flags");

            _finishAction = (context, information) =>
            {
                finishActionLogged = true;
                applicationName = information.ApplicationName;
                eventName = information.EventName;
                exception = information.Exception;
                flags = information.Flags;
                methodsIdentifier = information.MethodIdentifier;
                parentSpanId = information.ParentSpanId;
                sampled = information.Sampled;
                spanId = information.SpanId;
                traceId = information.TraceId;
                httpRequest = information.Request;
                httpResponse = information.Response;
                responseTime = information.ResponseTime;
            };

            _testApmWebApiFilterAttribute = new TestApmWebApiFilterAttribute(_applicationName, _addResponseHeaders, _startAction, _finishAction);
            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            var actionExecutedContext = new HttpActionExecutedContext(actionContext, new Exception("Exception occured"));

            _testApmWebApiFilterAttribute.OnActionExecuted(actionExecutedContext);

            Assert.IsNotNull(httpRequest);
            //Assert.IsNotNull(httpResponse);
            Assert.IsNotNull(exception);
            Assert.IsTrue(finishActionLogged);
            Assert.Greater(responseTime, 0);
            Assert.IsNotEmpty(applicationName);
            Assert.IsNotEmpty(eventName);
            Assert.AreEqual("TestClient=1234", traceId);
            Assert.AreEqual("SpecialProcess=4321", spanId);
            Assert.AreEqual("ParentSpecialProcess=5678", parentSpanId);
            Assert.AreEqual("Sampled", sampled);
            Assert.AreEqual("Flags", flags);

            var controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
            var methodType = actionContext.Request.Method;
            var actionName = actionContext.ActionDescriptor.ActionName;
            var arguments = string.Empty;

            var expectedMethodIdentifier = string.Format("{0}.{1}({2}) - {3}", controllerName, actionName, arguments, methodType);

            Assert.AreEqual(expectedMethodIdentifier, methodsIdentifier);

        }

        [Test]
        public void WhenReceivingSampledHeader()
        {
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.SampledHeaderKey, "Sampled");

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.SampledHeaderKey));
            var sampled = (string)actionContext.Request.Properties[Constants.SampledHeaderKey];
            Assert.AreEqual("Sampled", sampled);
        }

        [Test]
        public void WhenReceivingNoSampledHeader()
        {
            var actionContext = ContextUtil.CreateActionContext();

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsFalse(actionContext.Request.Properties.ContainsKey(Constants.SampledHeaderKey));
        }

        [Test]
        public void WhenReceivingFlagsHeader()
        {
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.FlagsHeaderKey, "Flags");

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.FlagsHeaderKey));
            var flags = (string)actionContext.Request.Properties[Constants.FlagsHeaderKey];
            Assert.AreEqual("Flags", flags);
        }

        [Test]
        public void WhenReceivingNoFlagsHeader()
        {
            var actionContext = ContextUtil.CreateActionContext();

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsFalse(actionContext.Request.Properties.ContainsKey(Constants.FlagsHeaderKey));
        }

        [Test]
        public void WhenReceivingNoTracingInformation()
        {
            var actionContext = ContextUtil.CreateActionContext();

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.TraceIdHeaderKey));
            var traceId = (string)actionContext.Request.Properties[Constants.TraceIdHeaderKey];
            Assert.IsNotEmpty(traceId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.SpanIdHeaderKey));
            var spanId = (string)actionContext.Request.Properties[Constants.SpanIdHeaderKey];
            Assert.IsNotEmpty(spanId);

            Assert.AreEqual(traceId, spanId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.ParentSpanIdHeaderKey));
            var parentSpanId = (string)actionContext.Request.Properties[Constants.ParentSpanIdHeaderKey];
            Assert.AreEqual(ApmWebApiRequestDecorator.NoParent, parentSpanId);
        }

        [Test]
        public void WhenReceivingTracingInformationForTraceId()
        {
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.TraceIdHeaderKey, "TestClient=1234");

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.TraceIdHeaderKey));
            var traceId = (string)actionContext.Request.Properties[Constants.TraceIdHeaderKey];
            Assert.AreEqual("TestClient=1234", traceId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.SpanIdHeaderKey));
            var spanId = (string)actionContext.Request.Properties[Constants.SpanIdHeaderKey];
            Assert.IsNotEmpty(spanId);

            Assert.AreNotEqual(traceId, spanId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.ParentSpanIdHeaderKey));
            var parentSpanId = (string)actionContext.Request.Properties[Constants.ParentSpanIdHeaderKey];
            Assert.AreEqual(ApmWebApiRequestDecorator.NoParent, parentSpanId);
        }

        [Test]
        public void WhenReceivingTracingInformationForTraceIdAndSpanId()
        {
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.TraceIdHeaderKey, "TestClient=1234");
            actionContext.Request.Headers.Add(Constants.SpanIdHeaderKey, "SpecialProcess=4321");
            
            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.TraceIdHeaderKey));
            var traceId = (string)actionContext.Request.Properties[Constants.TraceIdHeaderKey];
            Assert.AreEqual("TestClient=1234", traceId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.SpanIdHeaderKey));
            var spanId = (string)actionContext.Request.Properties[Constants.SpanIdHeaderKey];
            Assert.AreEqual("SpecialProcess=4321", spanId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.ParentSpanIdHeaderKey));
            var parentSpanId = (string)actionContext.Request.Properties[Constants.ParentSpanIdHeaderKey];
            Assert.AreEqual(ApmWebApiRequestDecorator.NoParent, parentSpanId);
        }

        [Test]
        public void WhenReceivingTracingInformationForTraceIdAndSpanIdAndParentId()
        {
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.TraceIdHeaderKey, "TestClient=1234");
            actionContext.Request.Headers.Add(Constants.SpanIdHeaderKey, "SpecialProcess=4321");
            actionContext.Request.Headers.Add(Constants.ParentSpanIdHeaderKey, "ParentSpecialProcess=5678");

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.TraceIdHeaderKey));
            var traceId = (string)actionContext.Request.Properties[Constants.TraceIdHeaderKey];
            Assert.AreEqual("TestClient=1234", traceId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.SpanIdHeaderKey));
            var spanId = (string)actionContext.Request.Properties[Constants.SpanIdHeaderKey];
            Assert.AreEqual("SpecialProcess=4321", spanId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.ParentSpanIdHeaderKey));
            var parentSpanId = (string)actionContext.Request.Properties[Constants.ParentSpanIdHeaderKey];
            Assert.AreEqual("ParentSpecialProcess=5678", parentSpanId);
        }

        [Test]
        public void WhenReceivingTracingInformationForParentIdOnly()
        {
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.ParentSpanIdHeaderKey, "ParentSpecialProcess=5678");

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.TraceIdHeaderKey));
            var traceId = (string)actionContext.Request.Properties[Constants.TraceIdHeaderKey];
            Assert.IsNotEmpty(traceId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.SpanIdHeaderKey));
            var spanId = (string)actionContext.Request.Properties[Constants.SpanIdHeaderKey];
            Assert.IsNotEmpty(spanId);

            Assert.AreEqual(traceId, spanId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.ParentSpanIdHeaderKey));
            var parentSpanId = (string)actionContext.Request.Properties[Constants.ParentSpanIdHeaderKey];
            Assert.AreEqual(ApmWebApiRequestDecorator.NoParent, parentSpanId);
        }

        [Test]
        public void WhenReceivingTracingInformationForSpanIdOnly()
        {
            var actionContext = ContextUtil.CreateActionContext();
            actionContext.Request.Headers.Add(Constants.SpanIdHeaderKey, "SpecialProcess=4321");

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.TraceIdHeaderKey));
            var traceId = (string)actionContext.Request.Properties[Constants.TraceIdHeaderKey];
            Assert.IsNotEmpty(traceId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.SpanIdHeaderKey));
            var spanId = (string)actionContext.Request.Properties[Constants.SpanIdHeaderKey];
            Assert.IsNotEmpty(spanId);

            Assert.AreEqual(traceId, spanId);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.ParentSpanIdHeaderKey));
            var parentSpanId = (string)actionContext.Request.Properties[Constants.ParentSpanIdHeaderKey];
            Assert.AreEqual(ApmWebApiRequestDecorator.NoParent, parentSpanId);
        }

        [Test]
        public void WhenReceivingTracingInformationMethodIdentifierIsAdded()
        {
            var actionContext = ContextUtil.CreateActionContext();

            var controllerName = actionContext.ControllerContext.ControllerDescriptor.ControllerName;
            var methodType = actionContext.Request.Method;
            var actionName = actionContext.ActionDescriptor.ActionName;
            var arguments = string.Empty;

            var expectedMethodIdentifier = string.Format("{0}.{1}({2}) - {3}", controllerName, actionName, arguments, methodType);

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.MethodIdentifierPropertyKey));
            var methodIdentifier = (string)actionContext.Request.Properties[Constants.MethodIdentifierPropertyKey];
            Assert.AreEqual(expectedMethodIdentifier, methodIdentifier);
        }

        [Test]
        public void WhenReceivingTracingInformationEventNameIsAdded()
        {
            var actionContext = ContextUtil.CreateActionContext();

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.EventNamePropertyKey));
            var eventName = (string)actionContext.Request.Properties[Constants.EventNamePropertyKey];
            Assert.IsNotEmpty(eventName);
        }

        [Test]
        public void WhenReceivingTracingInformatioApplicationNameIsAdded()
        {
            var actionContext = ContextUtil.CreateActionContext();

            _testApmWebApiFilterAttribute.OnActionExecuting(actionContext);

            Assert.IsTrue(actionContext.Request.Properties.ContainsKey(Constants.ApplicationNamePropertyKey));
            var applicationName = (string)actionContext.Request.Properties[Constants.ApplicationNamePropertyKey];
            Assert.IsNotEmpty(applicationName);
        }
    }
}
