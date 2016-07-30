﻿using Shouldly;
using StructureMap.Pipeline;
using System;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace StructureMap.Testing
{
    public class SessionCacheTester
    {
        private readonly IBuildSession theResolverMock;
        private SessionCache theCache;
        private readonly IPipelineGraph thePipeline;
        private readonly IInstanceGraph instanceGraphMock;

        public SessionCacheTester()
        {
            theResolverMock = Substitute.For<IBuildSession>();

            theCache = new SessionCache(theResolverMock);

            thePipeline = Substitute.For<IPipelineGraph>();
            thePipeline.ToModel().Returns(new Container().Model);

            instanceGraphMock = Substitute.For<IInstanceGraph>();
            thePipeline.Instances.Returns(instanceGraphMock);
        }

        [Fact]
        public void get_instance_if_the_object_does_not_already_exist()
        {
            var instance = new ConfiguredInstance(typeof(Foo));

            var foo = new Foo();

            theResolverMock.ResolveFromLifecycle(typeof(IFoo), instance).Returns(foo);

            theCache.GetObject(typeof(IFoo), instance, new TransientLifecycle()).ShouldBeTheSameAs(foo);
        }

        [Fact]
        public void get_instance_if_the_object_is_unique_and_does_not_exist()
        {
            var instance = new ConfiguredInstance(typeof(Foo));
            instance.SetLifecycleTo(Lifecycles.Unique);

            var foo = new Foo();
            var foo2 = new Foo();

            theResolverMock.BuildUnique(typeof(IFoo), instance).Returns(foo, foo2);

            theCache.GetObject(typeof(IFoo), instance, new UniquePerRequestLifecycle()).ShouldBeTheSameAs(foo);
            theCache.GetObject(typeof(IFoo), instance, new UniquePerRequestLifecycle()).ShouldBeTheSameAs(foo2);
        }

        [Fact]
        public void get_instance_remembers_the_first_object_created()
        {
            var instance = new ConfiguredInstance(typeof(Foo));

            var foo = new Foo();

            theResolverMock.ResolveFromLifecycle(typeof(IFoo), instance).Returns(foo);

            theCache.GetObject(typeof(IFoo), instance, Lifecycles.Transient).ShouldBeTheSameAs(foo);
            theCache.GetObject(typeof(IFoo), instance, Lifecycles.Transient).ShouldBeTheSameAs(foo);
            theCache.GetObject(typeof(IFoo), instance, Lifecycles.Transient).ShouldBeTheSameAs(foo);
            theCache.GetObject(typeof(IFoo), instance, Lifecycles.Transient).ShouldBeTheSameAs(foo);
            theCache.GetObject(typeof(IFoo), instance, Lifecycles.Transient).ShouldBeTheSameAs(foo);

            theResolverMock.Received(1).ResolveFromLifecycle(typeof(IFoo), instance);

        }

        [Fact]
        public void get_default_if_it_does_not_already_exist()
        {
            var instance = new ConfiguredInstance(typeof(Foo));
            instanceGraphMock.GetDefault(typeof(IFoo)).Returns(instance);

            var foo = new Foo();

            theResolverMock.ResolveFromLifecycle(typeof(IFoo), instance).Returns(foo);

            theCache.GetDefault(typeof(IFoo), thePipeline)
                .ShouldBeTheSameAs(foo);
        }

        [Fact]
        public void get_default_is_cached()
        {
            var instance = new ConfiguredInstance(typeof(Foo));
            instanceGraphMock.GetDefault(typeof(IFoo)).Returns(instance);

            var foo = new Foo();

            theResolverMock.ResolveFromLifecycle(typeof(IFoo), instance).Returns(foo);

            theCache.GetDefault(typeof(IFoo), thePipeline).ShouldBeTheSameAs(foo);
            theCache.GetDefault(typeof(IFoo), thePipeline).ShouldBeTheSameAs(foo);
            theCache.GetDefault(typeof(IFoo), thePipeline).ShouldBeTheSameAs(foo);
            theCache.GetDefault(typeof(IFoo), thePipeline).ShouldBeTheSameAs(foo);

            theResolverMock.Received(1).ResolveFromLifecycle(typeof(IFoo), instance);
        }

        [Fact]
        public void start_with_explicit_args()
        {
            var foo1 = new Foo();

            var args = new ExplicitArguments();
            args.Set<IFoo>(foo1);

            theCache = new SessionCache(theResolverMock, args);

            instanceGraphMock.GetDefault(typeof(IFoo)).Throws(new NotImplementedException());

            theCache.GetDefault(typeof(IFoo), thePipeline)
                .ShouldBeTheSameAs(foo1);
        }

        [Fact]
        public void try_get_default_completely_negative_case()
        {
            theCache.TryGetDefault(typeof(IFoo), thePipeline).ShouldBeNull();
        }

        [Fact]
        public void try_get_default_with_explicit_arg()
        {
            var foo1 = new Foo();

            var args = new ExplicitArguments();
            args.Set<IFoo>(foo1);

            theCache = new SessionCache(theResolverMock, args);

            theCache.GetDefault(typeof(IFoo), thePipeline)
                .ShouldBeTheSameAs(foo1);
        }

        [Fact]
        public void try_get_default_with_a_default()
        {
            var instance = new ConfiguredInstance(typeof(Foo));
            instanceGraphMock.GetDefault(typeof(IFoo)).Returns(instance);

            var foo = new Foo();

            theResolverMock.ResolveFromLifecycle(typeof(IFoo), instance).Returns(foo);

            theCache.TryGetDefault(typeof(IFoo), thePipeline)
                .ShouldBeTheSameAs(foo);
        }

        [Fact]
        public void explicit_wins_over_instance_in_try_get_default()
        {
            var foo1 = new Foo();

            var args = new ExplicitArguments();
            args.Set<IFoo>(foo1);

            theCache = new SessionCache(theResolverMock, args);

            var instance = new ConfiguredInstance(typeof(Foo));
            instanceGraphMock.GetDefault(typeof(IFoo)).Returns(instance);

            var foo2 = new Foo();

            theResolverMock.ResolveFromLifecycle(typeof(IFoo), instance).Returns(foo2);

            theCache.GetDefault(typeof(IFoo), thePipeline)
                .ShouldBeTheSameAs(foo1);
        }

        [Fact]
        public void
            should_throw_configuration_exception_if_you_try_to_build_the_default_of_something_that_does_not_exist()
        {
            var ex =
                Exception<StructureMapConfigurationException>.ShouldBeThrownBy(
                    () => theCache.GetDefault(typeof(IFoo), thePipeline));

            ex.Context.ShouldBe(
                "There is no configuration specified for StructureMap.Testing.SessionCacheTester+IFoo");

            ex.Title.ShouldBe(
                "No default Instance is registered and cannot be automatically determined for type 'StructureMap.Testing.SessionCacheTester+IFoo'");
        }

        [Fact]
        public void
            should_throw_configuration_exception_if_you_try_to_build_the_default_when_there_is_configuration_by_no_default
            ()
        {
            var container = new Container(x =>
            {
                x.For<IFoo>().Add<Foo>().Named("one");
                x.For<IFoo>().Add<Foo>().Named("two");
            });

            var ex =
                Exception<StructureMapConfigurationException>.ShouldBeThrownBy(() => { container.GetInstance<IFoo>(); });

            ex.Context.ShouldContain(
                "No default instance is specified.  The current configuration for type StructureMap.Testing.SessionCacheTester+IFoo is:");
        }

        public interface IFoo
        {
        }

        public class Foo : IFoo
        {
        }
    }
}