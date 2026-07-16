import { Fragment, useEffect, useMemo, useState } from 'react';
import {observer} from 'mobx-react-lite';
import {ProjectsList} from "../Components/ProjectsList";
import {useParams, Link, useNavigate} from "react-router";
import {NoProjectSelected} from "../Components/NoProjectSelected";
import {HistoryPanel} from "../Components/HistoryPanel";
import {useTranslation} from "react-i18next";
import {appState} from "../App";
import {Page404} from "./Page404";
import {DefaultButton, PrimaryButton} from "@fluentui/react";
import {SearchableAndSortableTable} from "../Components/SearchableAndSortableTable";
import {SimpleDate} from "../Components/SimpleDate";
import {ConversionJob, IConversionJob} from "../Utilities/DataTypes/ConversionJob";
import {ApiCalls} from "../Utilities/ApiCalls";

export const ScheduleHistoryPage = observer(()=>{
    const {projectId, scheduleId} = useParams<{projectId: string; scheduleId: string}>();
    return (
        <Fragment>
            <ProjectsList/>
            {projectId && scheduleId ? <ScheduleHistoryPanel/> : <NoProjectSelected/>}
        </Fragment>
    );
})

const ScheduleHistoryPanel = observer(()=>{
    const {t} = useTranslation();
    const navigate = useNavigate();
    const {scheduleId, projectId} = useParams<{scheduleId: string; projectId: string}>();
    const [conversionJobs, setConversionJobs] = useState<ConversionJob[]>();
    const [limit, setLimit] = useState(50)
    const [offset, setOffset] = useState(0)

    
    const project = useMemo(()=>{
        const currentProject = appState.projects.find(val=>val.id === projectId);

        if(currentProject && !currentProject?.fetched){
            currentProject.getFolders();
        }

        return currentProject;
        //eslint-disable-next-line
    }, [projectId, appState.projects])


    useEffect(()=>{
        if(project){
            
        ApiCalls.GetScheduleConversionJobs({projectId: projectId!, scheduleId: scheduleId!, limit, offset})
            .then((conversionJobs)=>{
                setConversionJobs(conversionJobs.map(x=>new ConversionJob(x, project)));
            })
        }
    }, [scheduleId, project, limit])

    useEffect(()=>{
        if(project){
            
        ApiCalls.GetScheduleConversionJobs({projectId: projectId!, scheduleId: scheduleId!, limit, offset})
            .then((newConversionJobs)=>{
                setConversionJobs(conversionJobs?.concat(newConversionJobs.map(x=>new ConversionJob(x, project))));
            })
        }
    }, [offset])
    
    if(!project){
        return <Page404/>
    }

    return (
        <div id={"historyPanel"}>
            <header style={{padding: '10px'}}>
                <h2>{project?.name}</h2>
                <div className={"flexFiller"}/>

                <Link to={`/projects/${projectId}/${scheduleId ? `/schedules/${scheduleId}` : ""}`}>
                    <PrimaryButton text={t("Back to Folder Tree")}/>
                </Link>
            </header>
            <div style={{overflowY: 'auto', display: "flex", flexDirection: 'column'}}>
                <SearchableAndSortableTable
                    title={"Single Schedule Conversion History"}
                    selectedRowId={""}
                    items={conversionJobs ?? []}
                    sortedColumnKey={"jobCreated"}
                    sortedColumnKeyDirection={true}
                    columns={[
                        {key: 'fileName', fieldName: 'fileName', minWidth: 200, name: t("File Name")},
                        {key: 'schedule.name', fieldName: 'schedule.name', minWidth: 200, name: t("Schedule"), onRender: (item)=>{return item?.schedule?.name || t("Ad-Hoc")}},
                        {key: 'ifcSettingsSetName', fieldName: 'ifcSettingsSetName', minWidth: 200, name: t("IFC Setting Set")},
                        {key: 'status', fieldName: 'status', minWidth: 100, name: t("Status"), onRender: item=>t(item.status)},
                        {key: 'createdBy', fieldName: 'createdBy', minWidth: 200, name: t("Created By")},
                        {key: 'jobCreated', fieldName: 'jobCreated', minWidth: 100, data: 'dayjs', name: t("Created"), onRender: (item)=>(<SimpleDate date={item.jobCreated}/>)},
                        {key: 'jobFinished', fieldName: 'jobFinished', minWidth: 100, data: 'dayjs', name: t("Finished"), onRender: (item)=>(<SimpleDate date={item.jobFinished}/>)},
                    ]}
                    onItemInvoked={(item: ConversionJob)=>{
                        navigate(`/projects/${projectId}/history/${item.id}`)
                    }}/>
                <DefaultButton disabled={project?.fullyLoaded} onClick={()=>setOffset(offset + limit)} text={`Load Additional History`}/>
            </div>
        </div>);
})