namespace MyApp

open WebSharper.UI.Templating

type MainTemplate = Template<"Main.html", ClientLoad.FromDocument>

module Employees =
    open System

    type Department =
        | Engineering
        | Finance
        | Marketing
        | Operations
        | CustomerSupport
        | HumanResources
        | ProductManagement
        | Legal
        | Design
        | Sales

        override this.ToString() =
            match this with
            | Engineering -> "Engineering"
            | Finance -> "Finance"
            | Marketing -> "Marketing"
            | Operations -> "Operations"
            | CustomerSupport -> "Customer Support"
            | HumanResources -> "Human Resources"
            | ProductManagement -> "Product Management"
            | Legal -> "Legal"
            | Design -> "Design"
            | Sales -> "Sales"

    type Person =
        {
            FirstName: string
            LastName: string
            Age: int
            Department: Department
            StartDate: DateTime
        }

        static member Create (firstName, lastName, age, department, startDate) =
            {
                FirstName = firstName
                LastName = lastName
                Age = age
                Department = department
                StartDate = startDate
            }

    let headerColumn (col:string) =
        MainTemplate.EmployeesTable_HeaderColumn()
            .Header(col)
            .Doc()

    let row (person:Person) =
        MainTemplate.Employee()
            .FirstName(person.FirstName)
            .LastName(person.LastName)
            .Age(string person.Age)
            .Department(person.Department.ToString())
            .StartDate(person.StartDate.ToShortDateString())
            .Doc()

    let Data =
        MainTemplate.Employees()
            .HeaderRow([
                headerColumn "First Name"
                headerColumn "Last Name"
                headerColumn "Age"
                headerColumn "Department"
                headerColumn "Start Date"
            ])
            .Data([
                row <| Person.Create ("Anna", "Kovacs", 29, Department.Engineering, DateTime.Parse "2021-04-12")
            ])
            .Doc()
