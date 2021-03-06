﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NSpec;
using System.Collections.Specialized;
using Massive;
using System.IO;

namespace Oak.Tests
{
    class describe_ParamsModelBnder_for_json_input_stream : nspec
    {
        DynamicParams dynamicParams;

        dynamic asDynamic;

        MemoryStream stream;

        void specify_id_conversions_exit_if_the_value_is_already_converted()
        {
            stream = new MemoryStream();

            var streamWriter = new StreamWriter(stream);

            streamWriter.Write("{ Id: 1 }");

            streamWriter.Flush();

            dynamicParams = new DynamicParams(stream, null);

            asDynamic = dynamicParams;

            ((long)asDynamic.Id).should_be(1);
        }
    }

    class describe_ParamsModelBinder_for_name_value_collection : nspec
    {
        DynamicParams dynamicParams;

        NameValueCollection nameValueCollection;

        Seed seed;

        dynamic asDynamic;

        void before_each()
        {
            nameValueCollection = new NameValueCollection();
        }

        void act_each()
        {
            dynamicParams = new DynamicParams(nameValueCollection, null);

            asDynamic = dynamicParams;
        }

        void mvc_specific_exclusions()
        {
            before = () =>
            {
                nameValueCollection.Add("__RequestVerificationToken", "AAJKIJF121==");

                nameValueCollection.Add("Name", "123Foobar");
            };

            it["excludes the anti forgery form value (not need past the action filter)"] = () =>
                ((bool)asDynamic.RespondsTo("__RequestVerificationToken")).should_be_false();
        }

        void describe_casting_assumptions()
        {
            context["evaluating form collection for potential Int id candidate"] = () =>
            {
                context["name ends in Id"] = () =>
                {
                    before = () => nameValueCollection.Add("PersonId", "123");

                    it["comparing key's value with an int passes"] = () =>
                        ((int)asDynamic.PersonId).should_be(123);
                };

                context["name ends in Id but isn't an int"] = () =>
                {
                    before = () => nameValueCollection.Add("PersonId", "123Foobar");

                    it["keeps original value"] = () =>
                        ((string)asDynamic.PersonId).should_be("123Foobar");
                };

                context["values with leading zero's is supplied"] = () =>
                {
                    before = () => nameValueCollection.Add("PersonId", "000123");

                    it["converts to int"] = () =>
                        ((int)asDynamic.PersonId).should_be(123);
                };

                context["name ends in ID"] = () =>
                {
                    before = () => nameValueCollection.Add("PersonID", "123");

                    it["comparing key's value with an int passes"] = () =>
                        ((int)asDynamic.PersonID).should_be(123);
                };

                context["name contains ID"] = () =>
                {
                    before = () => nameValueCollection.Add("PersonIDFoobar", "123");

                    it["disregards conversion"] = () =>
                        (asDynamic.PersonIDFoobar as string).should_be("123");
                };
            };

            context["evaluating form collection for potential Guid id candidate"] = () =>
            {
                context["name ends in Id"] = () =>
                {
                    before = () => nameValueCollection.Add("PersonId", Guid.Empty.ToString());

                    it["comparing key's value with a guid passes"] = () =>
                        ((Guid)asDynamic.PersonId).should_be(Guid.Empty);
                };

                context["name ends in ID"] = () =>
                {
                    before = () => nameValueCollection.Add("PersonID", Guid.Empty.ToString());

                    it["comparing key's value with a guid passes"] = () =>
                        ((Guid)asDynamic.PersonId).should_be(Guid.Empty);
                };
            };

            context["evaluating form collection for potential null values"] = () =>
            {
                context["form collection contains a string that has the literal value of 'null'"] = () =>
                {
                    before = () =>
                    {
                        nameValueCollection.Add("FirstName", "null");
                        nameValueCollection.Add("LastName", null);
                    };

                    it["is set to null as opposed to the string value"] = () =>
                    {
                        ((object)asDynamic.FirstName).should_be(null);

                        ((object)asDynamic.LastName).should_be(null);
                    };
                };
            };
        }

        void saving_dynamic_params()
        {
            before = () =>
            {
                seed = new Seed();

                seed.PurgeDb();

                seed.CreateTable("Blogs", new dynamic[] 
                { 
                    new { Id = "int", Identity = true, PrimaryKey = true },
                    new { Title = "nvarchar(255)" }
                }).ExecuteNonQuery();

                nameValueCollection.Add("Title", "Some Title");
            };

            it["persists saveable values to the database"] = () =>
            {
                var blogs = new DynamicRepository("Blogs");

                var blogId = blogs.Insert(asDynamic);

                var blog = blogs.Single(blogId);

                (blog.Title as string).should_be("Some Title");
            };
        }

        void mass_assignment()
        {
            before = () =>
            {
                seed = new Seed();

                seed.PurgeDb();

                seed.CreateTable("Users", new dynamic[] 
                { 
                    new { Id = "int", Identity = true, PrimaryKey = true },
                    new { Name = "nvarchar(255)" },
                    new { IsAdmin = "bit", Default = false }
                }).ExecuteNonQuery();

                nameValueCollection.Add("Name", "John");

                nameValueCollection.Add("IsAdmin", "true");
            };

            it["allows the ability to exclude fields"] = () =>
            {
                var users = new DynamicRepository("Users");

                var userId = users.Insert(asDynamic.Exclude("IsAdmin"));

                var user = users.Single(userId);

                (user.Name as string).should_be("John");

                ((bool)user.IsAdmin).should_be(false);
            };

            it["allows the ability to select fields"] = () =>
            {
                var users = new DynamicRepository("Users");

                var userId = users.Insert(asDynamic.Select("Name"));

                var user = users.Single(userId);

                (user.Name as string).should_be("John");

                ((bool)user.IsAdmin).should_be(false);
            };
        }
    }
}
