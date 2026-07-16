import { useEffect, useMemo } from 'react';
import {observer} from 'mobx-react-lite';
import {useParams} from "react-router-dom";
import {appState} from "../App";
import {Page404} from "../Pages/Page404";
import {DefaultButton, PrimaryButton} from "@fluentui/react";
import { useTranslation } from 'react-i18next';
import {Link, useNavigate} from "react-router-dom";
import {SearchableAndSortableTable} from "./SearchableAndSortableTable";
import {SimpleDate} from "./SimpleDate";
import {ConversionJob} from "../Utilities/DataTypes/ConversionJob";

export const HistoryPanel = observer(()=>{
    const {t} = useTranslation();
    const navigate = useNavigate();
    const {scheduleId, projectId} = useParams<{scheduleId: string; projectId: string}>();

    const project = useMemo(()=>{
        const currentProject = appState.projects.find(val=>val.id === projectId);

        if(currentProject && !currentProject?.fetched){
            currentProject.getFolders();
        }

        return currentProject;
        //eslint-disable-next-line
    }, [projectId, appState.projects])

    useEffect(()=>{
        project?.getConversionJobs(true);
    }, [project])

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
                    title={"Conversion History"}
                    selectedRowId={""}
                    items={project.conversionJobs}
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
                 <DefaultButton disabled={project?.fullyLoaded} onClick={()=>project?.getConversionJobs()} text={`Load Additional History`}/>
            </div>
        </div>);
})