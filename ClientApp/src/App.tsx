import React from 'react';
import {Route, Switch, Redirect, useHistory} from 'react-router';

import {AppState} from "./Utilities/AppState";
import {SettingsPage} from "./Pages/SettingsPage";
import {MainPage} from "./Pages/MainPage";
import {Layout} from "./Components/Layout";
import { initializeIcons } from '@fluentui/react/lib/Icons';
import {LoadingPage} from "./Pages/LoadingPage";
import {LoginPage} from "./Pages/LoginPage";
import {observer} from "mobx-react-lite";
import {HistoryPage} from "./Pages/HistoryPage";
import 'mobx-react-lite/batchingForReactDom';
import {SingleHistoryPage} from "./Pages/SingleHistoryPage";
import {ScheduleHistoryPage} from "./Pages/ScheduleHistoryPage";

export const appState = new AppState();
initializeIcons();

export const App = observer(()=>{
    appState.history = useHistory();

    if(!appState.ready){
        return <LoadingPage/>
    } else if(!appState.user){
        return <LoginPage/>
    } else {
        return (
            <Layout>
                <Switch>
                    <Route exact path='/projects/:projectId/history/:conversionJobId' component={SingleHistoryPage} />
                    <Route exact path='/projects/:projectId/history' component={HistoryPage} />
                    <Route path='/projects/:projectId/schedules/:scheduleId/history' component={ScheduleHistoryPage} />
                    <Route path='/projects/:projectId?/(schedules)?/:scheduleId?' component={MainPage} />
                    <Route path='/settings' component={SettingsPage} />

                    <Redirect exact path='/' to={'/projects'} />
                    {/*<Route component={Page404}/>*/}
                </Switch>
            </Layout>
        );
    }
});
