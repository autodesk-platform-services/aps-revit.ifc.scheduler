import React from 'react';
import {Route, Routes, Navigate, useNavigate} from 'react-router-dom';

import {AppState} from "./Utilities/AppState";
import {SettingsPage} from "./Pages/SettingsPage";
import {MainPage} from "./Pages/MainPage";
import {Layout} from "./Components/Layout";
import { initializeIcons } from '@fluentui/react/lib/Icons';
import {LoadingPage} from "./Pages/LoadingPage";
import {LoginPage} from "./Pages/LoginPage";
import {observer} from "mobx-react-lite";
import {HistoryPage} from "./Pages/HistoryPage";
import {SingleHistoryPage} from "./Pages/SingleHistoryPage";
import {ScheduleHistoryPage} from "./Pages/ScheduleHistoryPage";

export const appState = new AppState();
initializeIcons();

export const App = observer(()=>{
    appState.navigate = useNavigate();

    if(!appState.ready){
        return <LoadingPage/>
    } else if(!appState.user){
        return <LoginPage/>
    } else {
        return (
            <Layout>
                <Routes>
                    <Route path='/projects/:projectId/history/:conversionJobId' element={<SingleHistoryPage/>} />
                    <Route path='/projects/:projectId/history' element={<HistoryPage/>} />
                    <Route path='/projects/:projectId/schedules/:scheduleId/history' element={<ScheduleHistoryPage/>} />
                    <Route path='/projects/:projectId/schedules/:scheduleId' element={<MainPage/>} />
                    <Route path='/projects/:projectId' element={<MainPage/>} />
                    <Route path='/projects' element={<MainPage/>} />
                    <Route path='/settings' element={<SettingsPage/>} />

                    <Route path='/' element={<Navigate to='/projects' replace/>} />
                    {/*<Route path='*' element={<Page404/>} />*/}
                </Routes>
            </Layout>
        );
    }
});
