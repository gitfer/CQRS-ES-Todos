﻿using CommonDomain.Persistence;
using FluentValidation;
using FluentValidation.Resources;
using System;
using System.Linq;
using Todo.Domain.Messages.Commands;
using Todo.Domain.Model;
using Todo.Infrastructure;
using Todo.QueryStack;

namespace Todo.CommandStack.Logic.Validators
{
    public class CreateToDoListCommandValidator : AbstractValidator<CreateToDoListCommand>
    {
        private readonly IDatabase database;

        public CreateToDoListCommandValidator(IDatabase db)
        {
            Contract.Requires<ArgumentNullException>(db != null, "db");
            database = db;

            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.Title).NotEmpty();
            RuleFor(command => command.Title).Must(BeUniqueTitle).WithMessage("List's Title is already used. Please choose another.");
        }

        private bool BeUniqueTitle(string title)
        {
            return !database.ToDoLists.Any(t => t.Title.Equals(title));
        }
    }

    public class AddNewToDoItemCommandValidator : AbstractValidator<AddNewToDoItemCommand>
    {
        private readonly IDatabase database;

        public AddNewToDoItemCommandValidator(IDatabase db)
        {
            Contract.Requires<ArgumentNullException>(db != null, "db");
            database = db;

            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.Description).NotEmpty();
            // If DueDate is not null, it should be >= CreationDate
            RuleFor(command => command.DueDate.Value.Date).GreaterThanOrEqualTo(command => command.CreationDate.Date).When(command => command.DueDate != null);
            RuleFor(command => command.Importance).GreaterThanOrEqualTo(0);
            // Importance must be >=0 and unique among other item's importance
            RuleFor(command => command.Importance).Must(BeUniqueAmongItemsImportance).WithMessage("{PropertyName} must be unique in the List");
        }

        private bool BeUniqueAmongItemsImportance(AddNewToDoItemCommand command, int importance)
        {
            int todoList = database.IdMaps.GetModelId<Todo.QueryStack.Model.ToDoList>(command.TodoListId);
            return !database.ToDoItems.Any(todo => todo.ToDoListId.Equals(todoList) && todo.Importance.Equals(importance));
        }
    }

    public class MarkToDoItemAsCompletedCommandValidator :AbstractValidator<MarkToDoItemAsCompleteCommand>
    {
        private readonly IRepository repository;

        public MarkToDoItemAsCompletedCommandValidator(IRepository repo)
        {
            Contract.Requires<ArgumentNullException>(repo != null, "repo");
            repository = repo;

            RuleFor(command => command.ClosingDate).Must(GreaterThanOrEqualToCreation)
                .WithMessage(Messages.greaterthanorequal_error, "ClosingDate", "CreationDate");

            //// Alternative way to implement same custom validation rule (more flexible, but for most of the cases "Must" is ok)
            //Custom(command =>
            //{
            //    ToDoItem item = repository.GetById<ToDoItem>(command.Id);
            //    return command.ClosingDate < item.CreationDate ?
            //        new ValidationFailure("ClosingDate","'ClosingDate' deve essere minore della data di creazione") :
            //        null;
            //});
        }

        private bool GreaterThanOrEqualToCreation(MarkToDoItemAsCompleteCommand command, DateTime closingDate)
        {
            ToDoItem item = repository.GetById<ToDoItem>(command.Id);
            return closingDate >= item.CreationDate;
        }
    }

    public class ReOpenToDoItemCommandValidator : AbstractValidator<ReOpenToDoItemCommand>
    {
        public ReOpenToDoItemCommandValidator()
        {
            RuleFor(command => command.Id).NotEmpty();
        }
    }

    public class ChangeToDoItemImportanceCommandValidator : AbstractValidator<ChangeToDoItemImportanceCommand>
    {
        private readonly IDatabase database;

        public ChangeToDoItemImportanceCommandValidator(IDatabase db)
        {
            Contract.Requires<ArgumentNullException>(db != null, "db");
            database = db;

            RuleFor(command => command.Importance).NotEmpty().GreaterThanOrEqualTo(0);
            // Importance must be >=0 and unique among other item's importance
            RuleFor(command => command.Importance).Must(BeUniqueAmongItemsImportance).WithMessage("{PropertyName} must be unique in the List");
        }

        private bool BeUniqueAmongItemsImportance(ChangeToDoItemImportanceCommand command, int importance)
        {
            int todoid = database.IdMaps.GetModelId<Todo.QueryStack.Model.ToDoItem>(command.Id);
            return (from todo in database.ToDoItems
                    join list in database.ToDoLists on todo.ToDoListId equals list.Id
                    where
                    todo.Importance == importance &&
                    list.Id == (from _todo in database.ToDoItems
                                where _todo.Id == todoid
                                select _todo.ToDoListId).FirstOrDefault()
                    select todo).Count() == 0;
        }
    }

    public class ChangeToDoItemDescriptionCommandValidator : AbstractValidator<ChangeToDoItemDescriptionCommand>
    {
        public ChangeToDoItemDescriptionCommandValidator()
        {
            RuleFor(command => command.Description).NotEmpty();
        }
    }

    public class ChangeToDoItemDueDateCommandValidator : AbstractValidator<ChangeToDoItemDueDateCommand>
    {
        public ChangeToDoItemDueDateCommandValidator()
        {
            // If DueDate is not null, it should be >= today
            RuleFor(command => command.DueDate.Value.Date).GreaterThanOrEqualTo(DateTime.Now.Date).When(command => command.DueDate != null);
        }
    }
}
