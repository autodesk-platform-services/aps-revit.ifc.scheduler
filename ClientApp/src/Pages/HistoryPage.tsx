import React from 'react';
import {observer} from 'mobx-react-lite';
import {ProjectsList} from "../Components/ProjectsList";
import {useParams} from "react-router-dom";
import {NoProjectSelected} from "../Components/NoProjectSelected";
import {HistoryPanel} from "../Components/HistoryPanel";

export const HistoryPage = observer(()=>{
    const {projectId} = useParams<{projectId: string}>();
    return <React.Fragment>
        <ProjectsList/>
        {projectId ? <HistoryPanel/> : <NoProjectSelected/>}
    </React.Fragment>
})