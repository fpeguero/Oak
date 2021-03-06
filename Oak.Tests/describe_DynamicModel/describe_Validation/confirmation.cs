﻿using System.Collections.Generic;
using System.Linq;
using NSpec;
using Oak.Tests.describe_DynamicModel.describe_Validation.Classes;
using Massive;

namespace Oak.Tests.describe_DynamicModel.describe_Validation
{
    class confirmation_for_dynamic_object : confirmation
    {
        private void before_each()
        {
            person = new Person();

            person.Email = "user@example.com";

            person.EmailConfirmation = "user@example.com";
        }
    }

    class confirmation_for_class_containing_auto_properties : confirmation
    {
        private void before_each()
        {
            person = new PersonWithAutoProps();

            person.Email = "user@example.com";

            person.EmailConfirmation = "user@example.com";
        }
    }

    class confirmation_for_dynamic_object_with_defferred_error_message : confirmation
    {
        private void before_each()
        {
            person = new PersonWithDeferredErrorMessage();

            person.Email = "user@example.com";

            person.EmailConfirmation = "user@example.com";
        }
    }

    abstract class confirmation : nspec
    {
        public Seed seed;

        public dynamic person;

        public bool isValid;

        public Persons persons;

        void before_each()
        {
            seed = new Seed();

            seed.PurgeDb();

            persons = new Persons();
        }

        void confirming_password_is_entered()
        {
            act = () => isValid = person.IsValid();

            context["given emails match"] = () =>
            {
                before = () =>
                {
                    person.Email = "user@example.com";
                    person.EmailConfirmation = "user@example.com";
                };

                it["is valid"] = () => isValid.should_be_true();
            };

            context["given emails do not match"] = () =>
            {
                before = () =>
                {
                    person.Email = "user@example.com";
                    person.EmailConfirmation = "dd";
                };

                it["is invalid"] = () => isValid.should_be_false();

                it["error message states that it's invalid"] = () => 
                {  
                    person.IsValid();
                    (person.FirstError() as string).should_be("Email requires confirmation.");
                };
            };
        }

        void saving_something_that_has_confirmation_to_the_database()
        {
            before = () =>
            {
                seed.CreateTable("Persons", new dynamic[]
                { 
                    new { Id = "int", PrimaryKey = true, Identity = true },
                    new { Email = "nvarchar(255)" }
                }).ExecuteNonQuery();
            };

            act = () =>
            {
                person.Email = "user@example.com";
                person.EmailConfirmation = "user@example.com";
            };

            it["requires the exclusion of confirmation properties"] = () =>
            {
                persons.Insert(person.Exclude("EmailConfirmation"));

                var firstPerson = persons.All().First();

                (firstPerson.Email as string).should_be(person.Email as string);
            };
        }
    }
}
