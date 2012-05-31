﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Web;
using Moq;
using RequestReduce.Configuration;
using RequestReduce.Handlers;
using RequestReduce.IOC;
using RequestReduce.Module;
using RequestReduce.Store;
using RequestReduce.Utilities;
using StructureMap;
using Xunit;
using System.IO;
using Xunit.Extensions;
using UriBuilder = RequestReduce.Utilities.UriBuilder;

namespace RequestReduce.Facts.Module
{
    public class RequestReduceModuleFacts : IDisposable
    {
        [Fact]
        public void WillGetAndSetResponseFilterIfHtmlContent()
        {
            var module = new RequestReduceModule();
            var context = new Mock<HttpContextBase>();
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.SpriteVirtualPath).Returns("/Virtual");
            context.Setup(x => x.Items.Contains(ResponseFilter.ContextKey)).Returns(false);
            context.Setup(x => x.Request.RawUrl).Returns("/content/blah");
            context.Setup(x => x.Response.ContentType).Returns("text/html");
            context.Setup(x => x.Request.QueryString).Returns(new NameValueCollection());
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<IHostingEnvironmentWrapper>().Use(new Mock<IHostingEnvironmentWrapper>().Object);
                x.For<AbstractFilter>().Use(new Mock<AbstractFilter>().Object);
            });

            module.InstallFilter(context.Object);

            context.VerifyGet(x => x.Response.Filter, Times.Once());
            context.VerifySet(x => x.Response.Filter = It.IsAny<Stream>(), Times.Once());
            RRContainer.Current = null;
        }

        [Fact]
        public void WillGetAndSetResponseFilterIfXHtmlContent()
        {
            var module = new RequestReduceModule();
            var context = new Mock<HttpContextBase>();
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.SpriteVirtualPath).Returns("/Virtual");
            context.Setup(x => x.Items.Contains(ResponseFilter.ContextKey)).Returns(false);
            context.Setup(x => x.Request.RawUrl).Returns("/content/blah");
            context.Setup(x => x.Response.ContentType).Returns("application/xhtml+xml");
            context.Setup(x => x.Request.QueryString).Returns(new NameValueCollection());
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<IHostingEnvironmentWrapper>().Use(new Mock<IHostingEnvironmentWrapper>().Object);
                x.For<AbstractFilter>().Use(new Mock<AbstractFilter>().Object);
            });

            module.InstallFilter(context.Object);

            context.VerifyGet(x => x.Response.Filter, Times.Once());
            context.VerifySet(x => x.Response.Filter = It.IsAny<Stream>(), Times.Once());
            RRContainer.Current = null;
        }

        [Fact]
        public void WillNotSetResponseFilterIfFaviconIco()
        {
            RRContainer.Current = null;
            var module = new RequestReduceModule();
            var context = new Mock<HttpContextBase>();
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.SpriteVirtualPath).Returns("/Virtual");
            context.Setup(x => x.Items.Contains(ResponseFilter.ContextKey)).Returns(false);
            context.Setup(x => x.Response.ContentType).Returns("text/html");
            context.Setup(x => x.Request.QueryString).Returns(new NameValueCollection());
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            context.Setup(x => x.Request.RawUrl).Returns("/favicon.ico");
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<AbstractFilter>().Use(new Mock<AbstractFilter>().Object);
            });

            module.InstallFilter(context.Object);

            context.VerifySet(x => x.Response.Filter = It.IsAny<Stream>(), Times.Never());
            RRContainer.Current = null;
        }

        [Theory]
        [InlineData("/Content", true)]
        [InlineData("http://localhost/Content", true)]
        public void WillNotSetCachabilityIfNotInRRPatOrStoreDoesNotSendContent(string path, bool contentSent)
        {
            var module = new RequestReduceModule();
            var context = new Mock<HttpContextBase>();
            context.Setup(x => x.Request.RawUrl).Returns("/RRContent/css.css");
            context.Setup(x => x.Request.Url).Returns(new Uri("http://localhost/RRContent/css.css"));
            context.Setup(x => x.Request.Headers).Returns(new NameValueCollection());
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            var cache = new Mock<HttpCachePolicyBase>();
            context.Setup(x => x.Response.Cache).Returns(cache.Object);
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.SpriteVirtualPath).Returns(path);
            var store = new Mock<IStore>();
            store.Setup(
                x => x.SendContent(It.IsAny<string>(), context.Object.Response)).
                Returns(contentSent);
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<IHostingEnvironmentWrapper>().Use(new Mock<IHostingEnvironmentWrapper>().Object);
                x.For<IStore>().Use(store.Object);
                x.For<IUriBuilder>().Use(new UriBuilder(config.Object));
                x.For<IHandlerFactory>().Use<HandlerFactory>();
            });

            module.HandleRRContent(context.Object);

            cache.Verify(x => x.SetCacheability(HttpCacheability.Public), Times.Never());
            RRContainer.Current = null;
        }

        [Fact]
        public void WillNotHandleRequestsNotInMyDirectory()
        {
            var module = new RequestReduceModule();
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.SpriteVirtualPath).Returns("/RRContent");
            var context = new Mock<HttpContextBase>();
            context.Setup(x => x.Request.RawUrl).Returns("/RRContents/someresource");
            context.Setup(x => x.Request.Headers).Returns(new NameValueCollection());
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            var store = new Mock<IStore>();
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<IHostingEnvironmentWrapper>().Use(new Mock<IHostingEnvironmentWrapper>().Object);
                x.For<IStore>().Use(store.Object);
                x.For<IUriBuilder>().Use<UriBuilder>();
                x.For<IHandlerFactory>().Use(new Mock<IHandlerFactory>().Object);
            });

            module.HandleRRContent(context.Object);

            store.Verify(x => x.SendContent(It.IsAny<string>(), It.IsAny<HttpResponseBase>()), Times.Never());
            RRContainer.Current = null;
        }

        [Fact]
        public void WillNotHandleRequestsInChildDirectory()
        {
            var module = new RequestReduceModule();
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.SpriteVirtualPath).Returns("/RRContent");
            var context = new Mock<HttpContextBase>();
            context.Setup(x => x.Request.RawUrl).Returns("/RRContent/child/someresource");
            context.Setup(x => x.Request.Headers).Returns(new NameValueCollection());
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            var store = new Mock<IStore>();
            var factory = new Mock<HandlerFactory>();
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<IHostingEnvironmentWrapper>().Use(new Mock<IHostingEnvironmentWrapper>().Object);
                x.For<IStore>().Use(store.Object);
                x.For<IUriBuilder>().Use<UriBuilder>();
                x.For<IHandlerFactory>().Use(new Mock<IHandlerFactory>().Object);
            });

            module.HandleRRContent(context.Object);

            store.Verify(x => x.SendContent(It.IsAny<string>(), It.IsAny<HttpResponseBase>()), Times.Never());
            RRContainer.Current = null;
        }

        [Fact]
        public void WillDelegateContentMappedToHandler()
        {
            var module = new RequestReduceModule();
            var handler = new DefaultHttpHandler();
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.SpriteVirtualPath).Returns("/RRContent");
            var context = new Mock<HttpContextBase>();
            context.Setup(x => x.Request.RawUrl).Returns("/content/someresource.less");
            context.Setup(x => x.Request.Url).Returns(new Uri("http://host/content/someresource.less"));
            context.Setup(x => x.Request.Headers).Returns(new NameValueCollection());
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            context.Setup(x => x.Items).Returns(new Hashtable());
            var handlerFactory = new HandlerFactory(config.Object, new UriBuilder(config.Object));
            handlerFactory.AddHandlerMap(x => x.AbsolutePath.EndsWith(".less") ? handler : null);
            handlerFactory.AddHandlerMap(x => x.AbsolutePath.EndsWith(".les") ? new DefaultHttpHandler() : null);
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<IUriBuilder>().Use<UriBuilder>();
                x.For<IHandlerFactory>().Use(handlerFactory);
            });

            module.HandleRRContent(context.Object);

            Assert.Equal(handler, context.Object.Items["remapped handler"]);
            RRContainer.Current = null;
        }

        [Fact]
        public void WillNotFlushReductionsIfNotOnFlushUrl()
        {
            var module = new RequestReduceModule();
            var config = new Mock<IRRConfiguration>();
            config.Setup(x => x.AuthorizedUserList).Returns(RRConfiguration.Anonymous);
            config.Setup(x => x.SpriteVirtualPath).Returns("/RRContent");
            var context = new Mock<HttpContextBase>();
            context.Setup(x => x.Request.RawUrl).Returns("/RRContent/notflush");
            var identity = new Mock<IIdentity>();
            identity.Setup(x => x.IsAuthenticated).Returns(false);
            context.Setup(x => x.User.Identity).Returns(identity.Object);
            context.Setup(x => x.Server).Returns(new Mock<HttpServerUtilityBase>().Object);
            var store = new Mock<IStore>();
            RRContainer.Current = new Container(x =>
            {
                x.For<IRRConfiguration>().Use(config.Object);
                x.For<IHostingEnvironmentWrapper>().Use(new Mock<IHostingEnvironmentWrapper>().Object);
                x.For<IStore>().Use(store.Object);
                x.For<IUriBuilder>().Use<UriBuilder>();
            });

            module.HandleRRFlush(context.Object);

            store.Verify(x => x.Flush(It.IsAny<Guid>()), Times.Never());
            RRContainer.Current = null;
        }

        public void Dispose()
        {
            RRContainer.Current = null;
        }
    }
} 