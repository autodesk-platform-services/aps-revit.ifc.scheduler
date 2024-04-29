# Revit to IFC Scheduler - Front End
[![node.js](https://img.shields.io/badge/Node.js-16.20.2-blue.svg)](https://nodejs.org)
[![License](https://img.shields.io/badge/License-Apache%202.0-yellowgreen.svg)](https://opensource.org/licenses/Apache-2.0)

[![react.js](https://img.shields.io/badge/React-20172A?style=for-the-badge&logo=react&logoColor=61DAFB)](https://react.dev/)
[![react router](https://img.shields.io/badge/React_Router-CA4245?style=for-the-badge&logo=react-router&logoColor=white)](https://reactrouter.com/)



Built using the Create-React-App utility.

## Setup

### Prerequisites

- [APS credentials](https://forge.autodesk.com/en/docs/oauth/v2/tutorials/create-app)
- [Node.js](https://nodejs.org) must be v16 at this moment
- [Yarn package manager](https://yarnpkg.com)
- Terminal (for example, [Windows Command Prompt](https://en.wikipedia.org/wiki/Cmd.exe) or [macOS Terminal](https://support.apple.com/guide/terminal/welcome/mac))

### Build the client side app

- Open Terminal
- Change folder to the [ClientApp](./ClientApp): `cd ClientApp`
- Install dependencies: `npm install` or `npm install --legacy-peer-deps` if npm shows errors on peer dependencies.
- Build the codes: `npm run build`
- Before starting the .NET app, ensure the environment variable be set as `PRODUCTION`. Otherwise, MSBuild will do `npm run build` again which will report errors.


Or,<br/><br/>
Just download the pre-build client side app from [here](../.readme/ClientApp-build-v2.zip), unzip it into the `ClientApp` folder and rename the folder as `build`.

## License

This application is licensed under Apache 2.0. For details, please see [LICENSE.md](../LICENSE.md).

## Written By

* Daniel Clayson, Global Consulting Delivery Team, Autodesk
* Reviewed and maintained by Eason Kang [in/eason-kang-b4398492](https://www.linkedin.com/in/eason-kang-b4398492), [Developer Advocate](http://aps.autodesk.com)