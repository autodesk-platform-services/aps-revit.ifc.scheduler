import { Fragment, useMemo, useState } from 'react';
import {observer} from 'mobx-react-lite';
import {SchedulePanel} from "./SchedulePanel";
import {useParams, Link} from "react-router";
import {ProjectFoldersAndFiles} from "./ProjectFoldersAndFiles";
import {appState} from "../App";
import {Page404} from "../Pages/Page404";
import {DefaultButton, PrimaryButton} from "@fluentui/react";
import { useTranslation } from 'react-i18next';
import {ConvertToIfcNowModal} from "./ConvertToIfcNowModal";
import {Project} from "../Utilities/DataTypes/Project";

export const ProjectPanel = observer(()=>{
    const {t} = useTranslation();
    const {scheduleId, projectId} = useParams<{scheduleId: string; projectId: string}>();
    const [showConvertModal, setShowConvertModal] = useState(false)


    const project = useMemo(()=>{
        const currentProject = appState.projects.find(val=>val.id === projectId);

        if(currentProject && !currentProject?.fetched){
            currentProject.getFolders();
        }

        return currentProject;
        //eslint-disable-next-line
    }, [projectId, appState.projects])

    const schedules = project?.schedules;
    const schedule = useMemo(()=>{
        return project?.schedules.find(val=>val.id === scheduleId);
        //eslint-disable-next-line
    }, [project, schedules, scheduleId])

    function createSchedule(project: Project){
        let name = t("Daily");
        var names = project.schedules.map(val=>val.name)
            .filter(val=>val.indexOf(name) > -1)

        if(names.length){
            name += ` (${names.length})`
        }
        project.createSchedule(name);

    }

    if(!project){
        return <Page404/>
    }

    return (
        <div id={"projectPanel"}>
            <header style={{display: "flex", boxSizing: 'border-box'}}>
                <h2>{project?.name}</h2>
                <div className={"flexFiller"}/>

                <Link to={`/projects/${projectId}/${scheduleId ? `schedules/${scheduleId}/history` : "history"}`}>
                    <PrimaryButton text={t(scheduleId ? "Schedule History" : "Conversion History")}/>
                </Link>

                {schedule ? <div/>: <DefaultButton text={t("Create Scheduled Conversion")} onClick={()=>createSchedule(project)}/>}
                <DefaultButton text={t("Convert Selected to IFC Now")} onClick={()=>setShowConvertModal(true)}/>
            </header>
            <section>
                {schedule ?
                    <Fragment>
                        <SchedulePanel schedule={schedule}/>
                        <ProjectFoldersAndFiles
                            project={project}
                            expanded={project.expanded}
                            setExpanded={project.setExpanded}
                            checked={schedule.checked}
                            setChecked={schedule.setChecked}
                        />
                    </Fragment>
                    :
                    <ProjectFoldersAndFiles
                        project={project}
                        expanded={project.expanded}
                        setExpanded={project.setExpanded}
                        checked={project.checked}
                        setChecked={project.setChecked}
                    />
                }
            </section>
            <ConvertToIfcNowModal project={project} schedule={schedule} show={showConvertModal} setShow={setShowConvertModal}/>
        </div>
    );
})