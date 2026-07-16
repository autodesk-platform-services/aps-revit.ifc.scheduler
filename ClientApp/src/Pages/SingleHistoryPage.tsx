import {observer} from "mobx-react-lite";
import {useParams} from "react-router-dom";
import React from "react";
import {ProjectsList} from "../Components/ProjectsList";
import {NoProjectSelected} from "../Components/NoProjectSelected";
import {SingleHistoryPanel} from "../Components/SingleHistoryPanel";

export const SingleHistoryPage = observer(()=>{
    const {conversionJobId} = useParams<{conversionJobId: string}>();
    return <React.Fragment>
        <ProjectsList/>
        {conversionJobId ? <SingleHistoryPanel/> : <NoProjectSelected/>}
    </React.Fragment>
})