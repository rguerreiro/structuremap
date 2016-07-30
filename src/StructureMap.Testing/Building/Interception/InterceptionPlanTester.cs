﻿using NSubstitute;
using Shouldly;
using StructureMap.Building;
using StructureMap.Building.Interception;
using StructureMap.Diagnostics;
using StructureMap.Testing.Pipeline;
using Xunit;

namespace StructureMap.Testing.Building.Interception
{
    public class InterceptionPlanTester
    {
        private readonly IBuildPlanVisitor theVisitor;

        public InterceptionPlanTester()
        {
            theVisitor = Substitute.For<IBuildPlanVisitor>();
        }

        [Fact]
        public void will_not_accept_decorator_that_does_not_match_the_plugin_type()
        {
            var target = new Target();
            var inner = Constant.For(target);

            Exception<StructureMapBuildPlanException>.ShouldBeThrownBy(() =>
            {
                new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                    new IInterceptor[]
                    {
                        new DecoratorInterceptor(typeof (ATarget), typeof (ATarget))
                    });
            });
        }

        public class ATarget : ITarget
        {
            public void Activate()
            {
                throw new System.NotImplementedException();
            }

            public void Debug()
            {
                throw new System.NotImplementedException();
            }
        }

        [Fact]
        public void intercept_happy_path_with_a_single_activation()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                new IInterceptor[]
                {
                    new ActivatorInterceptor<ITarget>(x => x.Activate())
                });

            var session = new StubBuildSession();
            plan.ToBuilder<ITarget>()(session, session)
                .ShouldBeTheSameAs(target);

            target.HasBeenActivated.ShouldBeTrue();
        }

        [Fact]
        public void intercept_happy_path_with_a_single_activation_that_uses_session()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                new IInterceptor[] { new ActivatorInterceptor<Target>((session, x) => x.UseSession(session)) });

            var theSession = new StubBuildSession();
            plan.ToBuilder<ITarget>()(theSession, theSession)
                .ShouldBeTheSameAs(target);

            target.Session.ShouldBeTheSameAs(theSession);
        }

        [Fact]
        public void intercept_sad_path_with_a_single_activation_that_uses_session()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                new IInterceptor[] { new ActivatorInterceptor<Target>((session, x) => x.BlowUpOnSession(session)) });

            var theSession = new StubBuildSession();

            var ex =
                Exception<StructureMapInterceptorException>.ShouldBeThrownBy(
                    () => { plan.ToBuilder<ITarget>()(theSession, theSession); });

            ex.Message.ShouldContain("Target.BlowUpOnSession(IContext)");
        }

        [Fact]
        public void multiple_activators_taking_different_accept_types()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                new IInterceptor[]
                {
                    new ActivatorInterceptor<ITarget>(x => x.Activate()),
                    new ActivatorInterceptor<Target>(x => x.TurnGreen())
                });

            var session = new StubBuildSession();
            plan.ToBuilder<ITarget>()(session, session)
                .ShouldBeTheSameAs(target);

            target.HasBeenActivated.ShouldBeTrue();
            target.Color.ShouldBe("Green");
        }

        [Fact]
        public void activator_that_fails_gets_wrapped_in_descriptive_text()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var interceptor = new ActivatorInterceptor<Target>(x => x.ThrowUp());
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                new IInterceptor[]
                {
                    interceptor
                });

            var ex =
                Exception<StructureMapInterceptorException>.ShouldBeThrownBy(
                    () =>
                    {
                        var session = new StubBuildSession();
                        plan.ToBuilder<ITarget>()(session, session);
                    });

            ex.Title.Trim().ShouldBe(
                "Activator interceptor failed during object creation.  See the inner exception for details.");
            ex.Message.ShouldContain(interceptor.Description);
        }

        [Fact]
        public void single_decorator_happy_path_that_uses_the_plugin_type()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator = new FuncInterceptor<ITarget>(t => new DecoratedTarget(t));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(), new IInterceptor[] { decorator });

            var session = new StubBuildSession();
            plan.ToBuilder<ITarget>()(session, session)
                .ShouldBeOfType<DecoratedTarget>()
                .Inner.ShouldBeTheSameAs(target);
        }

        [Fact]
        public void multiple_decorators_happy_path()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator1 = new FuncInterceptor<ITarget>(t => new DecoratedTarget(t));
            var decorator2 = new FuncInterceptor<ITarget>(t => new BorderedTarget(t));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                new IInterceptor[] { decorator1, decorator2 });

            var session = new StubBuildSession();
            plan.ToBuilder<ITarget>()(session, session)
                .ShouldBeOfType<BorderedTarget>()
                .Inner.ShouldBeOfType<DecoratedTarget>()
                .Inner.ShouldBeTheSameAs(target);
        }

        [Fact]
        public void mixed_activators_and_decorators_happy_path()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator1 = new FuncInterceptor<ITarget>(t => new DecoratedTarget(t));
            var decorator2 = new FuncInterceptor<ITarget>(t => new BorderedTarget(t));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(), new IInterceptor[]
            {
                decorator1,
                decorator2,
                new ActivatorInterceptor<ITarget>(x => x.Activate()),
                new ActivatorInterceptor<Target>(x => x.TurnGreen())
            });

            var session = new StubBuildSession();
            plan.ToBuilder<ITarget>()(session, session)
                .ShouldBeOfType<BorderedTarget>()
                .Inner.ShouldBeOfType<DecoratedTarget>()
                .Inner.ShouldBeTheSameAs(target);

            target.Color.ShouldBe("Green");
            target.HasBeenActivated.ShouldBeTrue();
        }

        [Fact]
        public void single_decorator_sad_path_that_uses_the_plugin_type()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator = new FuncInterceptor<ITarget>(t => new ThrowsDecoratedTarget(t));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(), new IInterceptor[] { decorator });

            var ex =
                Exception<StructureMapInterceptorException>.ShouldBeThrownBy(
                    () =>
                    {
                        var session = new StubBuildSession();
                        plan.ToBuilder<ITarget>()(session, session);
                    });

            ex.Message.ShouldContain("new ThrowsDecoratedTarget(ITarget)");
        }

        [Fact]
        public void single_decorator_happy_path_that_uses_the_plugin_type_and_session()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator = new FuncInterceptor<ITarget>((s, t) => new ContextKeepingTarget(s, t));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(), new IInterceptor[] { decorator });

            var theSession = new StubBuildSession();
            var result = plan.ToBuilder<ITarget>()(theSession, theSession)
                .ShouldBeOfType<ContextKeepingTarget>();

            result.Inner.ShouldBeTheSameAs(target);
            result.Session.ShouldBeTheSameAs(theSession);
        }

        [Fact]
        public void single_decorator_sad_path_that_uses_the_plugin_type_and_session()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator = new FuncInterceptor<ITarget>((s, t) => new SadContextKeepingTarget(s, t));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(), new IInterceptor[] { decorator });

            var theSession = new StubBuildSession();

            var ex =
                Exception<StructureMapInterceptorException>.ShouldBeThrownBy(
                    () => { plan.ToBuilder<ITarget>()(theSession, theSession); });

            ex.Message.ShouldContain("new SadContextKeepingTarget(IContext, ITarget)");
        }

        [Fact]
        public void accept_visitor_for_activator()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var interceptor = new ActivatorInterceptor<Target>(x => x.ThrowUp());
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(),
                new IInterceptor[]
                {
                    interceptor
                });

            plan.AcceptVisitor(theVisitor);

            theVisitor.Received().Activator(interceptor);
        }

        [Fact]
        public void accept_visitor_for_func_decorator()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator = new FuncInterceptor<ITarget>((s, t) => new ContextKeepingTarget(s, t));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(), new IInterceptor[] { decorator });

            plan.AcceptVisitor(theVisitor);

            theVisitor.Received().Decorator(decorator);
        }

        [Fact]
        public void accept_visitor_for_DependencyInterceptor()
        {
            var target = new Target();
            var inner = Constant.For(target);

            var decorator = new DecoratorInterceptor(typeof(ITarget), typeof(DecoratedTarget));
            var plan = new InterceptionPlan(typeof(ITarget), inner, Policies.Default(), new IInterceptor[] { decorator });

            plan.AcceptVisitor(theVisitor);

            theVisitor.Received().Decorator(decorator);
        }
    }
}