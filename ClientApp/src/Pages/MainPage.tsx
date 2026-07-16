import React from 'react';
import {observer} from 'mobx-react-lite';
import {ProjectsList} from "../Components/ProjectsList";
import {ProjectPanel} from "../Components/ProjectPanel";
import {useParams} from "react-router-dom";
import {ScheduleList} from "../Components/ScheduleList";
import {NoProjectSelected} from "../Components/NoProjectSelected";

export const MainPage = observer(()=>{
    const {projectId} = useParams<{projectId: string}>();
    return <React.Fragment>
        <ProjectsList/>
        {projectId
            ? <React.Fragment>
                <ScheduleList/>
                <ProjectPanel/>
            </React.Fragment>
            : <NoProjectSelected/>}

    </React.Fragment>
})