# Group Clashes

A Navisworks Manage plugin for rule-based clash results grouping.

## Getting Started

This plugin will help you to group clash result in a given Navisworks clash test. For now, you can group clashes by:
* Nearest level
* Nearest grid intersection
* Element belonging to a model
* Status, approval or assignment

Open the Group Clashes window by clicking on the Group Clashes button
Select a clash test. The two selections involved in the clash test are displayed below.
Select one or two clash rule. Since Navisworks Manage does not support subgroup for clashes, the group name will include the result of the two rules.
Click on Group.
For a full description of each grouping rule, visit [bim42.com](http://bim42.com/2016/10/group-clashes/).

### Prerequisities

This application is devellop with Visual Studio 2015.
To run this plugin, you will need Navisworks Manage 2015, 2016 or 2017. Just compile the solution and paste the resulting dll in the Navisworks Manage Plugin folder.
Before modifying anything, I advise you to check the Navisworks Manage SDK on the [Autodesk Developer Network](http://usa.autodesk.com/adsk/servlet/index?id=15024694&siteID=123112).

## Built With

* [Visual Studio 2015](https://www.visualstudio.com/vs/community/) - My tool of choice

## Contributing

Please feel free to submit pull request in any manner you want.

## Authors

* **Simon Moreau** - *Initial work* - [BIM 42](https://bim42.com)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Thanks to the team behind the Navisworks Manage SDK for providing me the idea with the tools to implementing it.

