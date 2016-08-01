using Shouldly;
using System;
using Xunit;

namespace StructureMap.Testing
{
    public class StructureMapConfigurationTester
    {
        public class WebRegistry : Registry
        {
        }

        public class CoreRegistry : Registry
        {
        }

        // Guid test based on problems encountered by Paul Segaro. See http://groups.google.com/group/structuremap-users/browse_thread/thread/34ddaf549ebb14f7?hl=en
        [Fact]
        public void TheDefaultInstanceIsALambdaForGuidNewGuid()
        {
            var container = new Container(x => x.For<Guid>().Use(() => Guid.NewGuid()));
            container.GetInstance<Guid>().ShouldNotBe(Guid.Empty);
        }

        [Fact]
        public void TheDefaultInstance_has_a_dependency_upon_a_Guid_NewGuid_lambda_generated_instance()
        {
            var container = new Container(x =>
            {
                x.For<Guid>().Use(() => Guid.NewGuid());
                x.For<IFoo>().Use<Foo>();
            });

            container.GetInstance<IFoo>().SomeGuid.ShouldNotBe(Guid.Empty);
        }
    }

    public interface IFoo
    {
        Guid SomeGuid { get; set; }
    }

    public class Foo : IFoo
    {
        public Foo(Guid someGuid)
        {
            SomeGuid = someGuid;
        }

        #region IFoo Members

        public Guid SomeGuid { get; set; }

        #endregion IFoo Members
    }

    public interface ISomething
    {
    }

    public class Something : ISomething
    {
        public Something()
        {
            throw new Exception("You can't make me!");
        }
    }
}