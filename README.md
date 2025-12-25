# Tabler

Tabler is a WebSharper F# source code generator (SCG) for annotated CSV to WebSharper `Doc` conversion. It automatically transforms `.table` files (which are CSV files with a special comment header) added to a WebSharper project to F# code that implements a server-side rendered, template-based table with the CSV data. 

## A sample `.table` file

```csv
# MyApp.Employees.Data = Person list {
#   "First Name"  -> FirstName: string
#   "Last Name"   -> LastName: string
#   "Age"		  -> Age: int
#   "Department"  -> Department: union
#   "Start Date"  -> StartDate: date
# }
First Name, Last Name, Age, Department, Start Date
Anna,Kovacs,29,Engineering,2021-04-12
Mark,Stevens,41,Finance,2016-09-01
Lucia,Ramirez,34,Marketing,2019-02-18
Peter,Nagy,38,Operations,2017-06-05
Sofia,Morales,26,Customer Support,2023-01-09
Daniel,Weber,45,Human Resources,2014-11-24
Emily,Chen,31,Product Management,2020-08-17
Robert,Klein,52,Legal,2010-03-22
Isabel,Fernandez,28,Design,2022-05-30
Thomas,OConnor,36,Sales,2018-10-08
```

## Sample output in WebSharper apps

![Sample rendering](table.png)

## The generated code

```fsharp
namespace MyApp

open WebSharper.UI.Templating

type MainTemplate=Template<"Main.html", ClientLoad.FromDocument>

module Employees =
    open System
    
    type Department = 
        | CustomerSupport
        | Design
        | Engineering
        | Finance
        | HumanResources
        | Legal
        | Marketing
        | Operations
        | ProductManagement
        | Sales
    
        override this.ToString() =
            match this with
                | CustomerSupport -> "Customer Support"
                | Design -> "Design"
                | Engineering -> "Engineering"
                | Finance -> "Finance"
                | HumanResources -> "Human Resources"
                | Legal -> "Legal"
                | Marketing -> "Marketing"
                | Operations -> "Operations"
                | ProductManagement -> "Product Management"
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
    
    let headerColumn (col: string) =
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
                row <| Person.Create ("Mark", "Stevens", 41, Department.Finance, DateTime.Parse "2016-09-01")
                row <| Person.Create ("Lucia", "Ramirez", 34, Department.Marketing, DateTime.Parse "2019-02-18")
                row <| Person.Create ("Peter", "Nagy", 38, Department.Operations, DateTime.Parse "2017-06-05")
                row <| Person.Create ("Sofia", "Morales", 26, Department.CustomerSupport, DateTime.Parse "2023-01-09")
                row <| Person.Create ("Daniel", "Weber", 45, Department.HumanResources, DateTime.Parse "2014-11-24")
                row <| Person.Create ("Emily", "Chen", 31, Department.ProductManagement, DateTime.Parse "2020-08-17")
                row <| Person.Create ("Robert", "Klein", 52, Department.Legal, DateTime.Parse "2010-03-22")
                row <| Person.Create ("Isabel", "Fernandez", 28, Department.Design, DateTime.Parse "2022-05-30")
                row <| Person.Create ("Thomas", "OConnor", 36, Department.Sales, DateTime.Parse "2018-10-08")
            ])
            .Count(string 10)
            .Doc()
    
    let DataCount = 10
```

## Learn more

For a blog article that introduced Tabler, go [here]().