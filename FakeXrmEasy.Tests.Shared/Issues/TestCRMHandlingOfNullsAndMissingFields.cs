using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FakeXrmEasy.Tests.Issues
{
    public class TestCRMHandlingOfNullsAndMissingFields
    {
        [Fact]
        public void TestRetrieveWithNull()
        {
            Entity testEntity = new Entity("testentity");
            testEntity["field"] = null;
            testEntity.Id = Guid.NewGuid();

            XrmFakedContext context = new XrmFakedContext();
            IOrganizationService service = context.GetOrganizationService();

            context.Initialize(
                new List<Entity>()
                {
                    testEntity
                }
            );

            Entity e = service.Retrieve("testentity", testEntity.Id, new ColumnSet("field"));
            Assert.False(e.Contains("field"));
        }

        [Fact]
        public void TestRetrieveMultipleWithNull()
        {
            Entity testEntity = new Entity("testentity");
            testEntity["field"] = null;
            testEntity.Id = Guid.NewGuid();

            XrmFakedContext context = new XrmFakedContext();
            IOrganizationService service = context.GetOrganizationService();

            context.Initialize(
                new List<Entity>()
                {
                    testEntity
                }
            );

            QueryExpression contactQuery = new QueryExpression("testentity");
            contactQuery.ColumnSet = new ColumnSet("field");
            EntityCollection result = service.RetrieveMultiple(contactQuery);
            Assert.False(result.Entities[0].Contains("field"));
        }

        [Fact]
        public void TestRetrieveWithMissingField()
        {
            Entity testEntity = new Entity("testentity");
            testEntity.Id = Guid.NewGuid();

            XrmFakedContext context = new XrmFakedContext();
            IOrganizationService service = context.GetOrganizationService();

            context.Initialize(
                new List<Entity>()
                {
                    testEntity
                }
            );

            Entity e = service.Retrieve("testentity", testEntity.Id, new ColumnSet("field"));
            Assert.False(e.Contains("field"));
        }

        [Fact]
        public void TestRetrieveMultipleWithMissingField()
        {
            Entity testEntity = new Entity("testentity");
            testEntity.Id = Guid.NewGuid();

            XrmFakedContext context = new XrmFakedContext();
            IOrganizationService service = context.GetOrganizationService();

            context.Initialize(
                new List<Entity>()
                {
                    testEntity
                }
            );

            QueryExpression contactQuery = new QueryExpression("testentity");
            contactQuery.ColumnSet = new ColumnSet("field");
            EntityCollection result = service.RetrieveMultiple(contactQuery);
            Assert.False(result.Entities[0].Contains("field"));
        }

        [Fact]
        public void TestRetriveWithLinkEntityWithNullField()
        {
            List<Entity> initialEntities = new List<Entity>();
            Entity parentEntity = new Entity("parent");
            parentEntity["field"] = null;
            // So there seems to be a bug here that if an entity only contains null fields that this entity won't be returned in a link entity query
            // The other field is to get around that
            parentEntity["otherfield"] = 1;
            parentEntity.Id = Guid.NewGuid();
            initialEntities.Add(parentEntity);

            Entity childEntity = new Entity("child");
            childEntity["parent"] = parentEntity.ToEntityReference();
            childEntity.Id = Guid.NewGuid();
            initialEntities.Add(childEntity);

            XrmFakedContext context = new XrmFakedContext();
            IOrganizationService service = context.GetOrganizationService();

            context.Initialize(initialEntities);

            QueryExpression query = new QueryExpression("child");
            LinkEntity link = new LinkEntity("child", "parent", "parent", "parentid", JoinOperator.Inner);
            link.EntityAlias = "parententity";
            link.Columns = new ColumnSet("field");
            query.LinkEntities.Add(link);

            Entity result = service.RetrieveMultiple(query).Entities[0];

            Assert.False(result.Contains("parententity.field"));
        }

        [Fact]
        public void TestRetriveMultipleWithLinkEntityWithAlternateNullField()
        {
            // ARRANGE

            List<Entity> initialEntities = new List<Entity>();

            Entity parentEntity = new Entity("parent");
            parentEntity["parentname"] = "parent name";
            parentEntity.Id = Guid.NewGuid();
            initialEntities.Add(parentEntity);

            // create the first child which has the "myvalue" field set to "value"
            Entity childEntity1 = new Entity("child");
            childEntity1["parent"] = parentEntity.ToEntityReference();
            childEntity1["name"] = "entity1";
            childEntity1["myvalue"] = "value";
            childEntity1.Id = Guid.NewGuid();
            initialEntities.Add(childEntity1);

            // create the second child which has the "myvalue" field set to null
            Entity childEntity2 = new Entity("child");
            childEntity2["parent"] = parentEntity.ToEntityReference();
            childEntity2["name"] = "entity2";
            childEntity2["myvalue"] = null;
            childEntity2.Id = Guid.NewGuid();
            initialEntities.Add(childEntity2);

            XrmFakedContext context = new XrmFakedContext();
            IOrganizationService service = context.GetOrganizationService();

            context.Initialize(initialEntities);

            // the query selects the "parent" entity, and joins to the "child" entities
            QueryExpression query = new QueryExpression("parent");
            query.ColumnSet = new ColumnSet("parentname");

            LinkEntity link = new LinkEntity("parent", "child", "parentid", "parent", JoinOperator.Inner);
            link.EntityAlias = "c";
            link.Columns = new ColumnSet("name", "myvalue");

            query.LinkEntities.Add(link);

            // ACT

            DataCollection<Entity> results = service.RetrieveMultiple(query).Entities;

            // ASSERT

            // fields for the first entity work as expected...
            string entity1Name = results[0].GetAttributeValue<AliasedValue>("c.name").Value as string;
            string entity1Value = results[0].GetAttributeValue<AliasedValue>("c.myvalue").Value as string;
            
            Assert.Equal("entity1", entity1Name);
            Assert.Equal("value", entity1Value);

            // fields for the second entity do not.  
            // The child "name" field is correct, but the "myvalue" field is returning the value of the previous
            // entity when it should be returning null
            string entity2Name = results[1].GetAttributeValue<AliasedValue>("c.name").Value as string;
            string entity2Value = results[1].GetAttributeValue<AliasedValue>("c.myvalue").Value as string;

            // this works fine:
            Assert.Equal("entity2", entity2Name);

            // this fails (entity2Value is "value")
            Assert.Equal(null, entity2Value);
        }
    }
}