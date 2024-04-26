import React, {useEffect, useMemo} from 'react';
import {observer} from 'mobx-react-lite';
import {useParams} from "react-router";
import {appState} from "../App";
import {Page404} from "../Pages/Page404";
import {DefaultButton, PrimaryButton} from "office-ui-fabric-react";
import { useTranslation } from 'react-i18next';
import {Link} from "react-router-dom";

export const SingleHistoryPanel = observer(()=>{
    const {t} = useTranslation();
    const {projectId, conversionJobId} = useParams();

    const project = useMemo(()=>{
        const currentProject = appState.projects.find(val=>val.id === projectId);

        if(currentProject && !currentProject?.fetched){
            currentProject.getFolders();
        }

        return currentProject;
        //eslint-disable-next-line
    }, [projectId, appState.projects])

    useEffect(()=>{
        project?.getConversionJobs();
    }, [project])


    const conversionJob = useMemo(()=>{
        return project?.conversionJobs.find(val=>val.id === conversionJobId);
        //eslint-disable-next-line
    }, [project, conversionJobId, project?.offset])

    // const topFolder = project?.folders[0];
    // const topFolderWebViewUrl = topFolder?.webView;
    // const webViewBaseUrl = topFolderWebViewUrl?.split('/folders/')[0];
    // console.log(webViewBaseUrl);
    // console.log(`${webViewBaseUrl}/folders/${conversionJob?.folderId}/detail`);
    // console.log(`${conversionJob?.folderUrl}`);
    // const webViewUrl = webViewBaseUrl ?
    //     `${webViewBaseUrl}/folders/${conversionJob?.folderId}/detail` :
    //         `${conversionJob?.folderUrl}`;
    const webViewUrl = conversionJob?.folderUrl;

    if(!project){
        return <Page404/>
    }

    return (
        <div id={"historyPanel"}>
            <header style={{padding: '10px'}}>
                <h2>{project?.name} > {conversionJob?.fileName}</h2>
                <a style={{marginLeft: '50px'}} target={"_blank"} rel={"noreferrer"} href={webViewUrl}>
                    <DefaultButton text={"Folder in ACC/BIM360"}/>
                </a>
                <div className={"flexFiller"}/>

                <Link to={`/projects/${projectId}/history`}>
                    <PrimaryButton text={t("Back to History")}/>
                </Link>
            </header>
            <div style={{overflowY: 'auto', display: "flex", flexDirection: 'column'}}>
                <div className={'workItemNoteContainer'}>
                    <WorkItemNote label={t("IFC Setting Set")} value={conversionJob?.ifcSettingsSetName}/>
                    <WorkItemNote label={t("Is Zip File")} value={conversionJob?.isCompositeDesign ? t("True") : t("False")}/>
                    <WorkItemNote label={t("Status")} value={conversionJob?.status}/>
                    <WorkItemNote label={t("Created By")} value={conversionJob?.createdBy}/>
                    <WorkItemNote label={t("Job Created")} value={conversionJob?.jobCreated?.toDate().toLocaleString()}/>
                    <WorkItemNote label={t("Job Finished")} value={conversionJob?.jobFinished?.toDate().toLocaleString()}/>

                </div>
                <div style={{padding: '25px', overflowY: "scroll", overflowX: 'hidden', whiteSpace: 'pre-wrap', overflowWrap: 'break-word', flex: 1}}>

                    <hr/>
                    <code>
                        {conversionJob?.notes}
                    </code>
                </div>
            </div>
        </div>);
})

const WorkItemNote = ({label, value}: {label: string, value?: string})=>{
    return <div className={'workItemNote'}><span className={'label'}>{label}:</span><span>{value || ""}</span></div>

}