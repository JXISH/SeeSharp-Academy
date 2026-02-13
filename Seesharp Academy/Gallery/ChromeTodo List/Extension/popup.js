// 全局变量
let tasks = [];
let currentFilter = 'all';
let currentSort = 'priority';
let contextMenuTargetId = null;

// 初始化
document.addEventListener('DOMContentLoaded', () => {
  setupEventListeners();
  setupContextMenu();
  loadTasks();
});

// 设置事件监听器
function setupEventListeners() {
  const addBtn = document.getElementById('addBtn');
  const taskInput = document.getElementById('taskInput');
  const filterBtns = document.querySelectorAll('.filter-btn');
  const sortSelect = document.getElementById('sortSelect');
  const addTaskToggle = document.getElementById('addTaskToggle');

  addBtn.addEventListener('click', addTask);
  
  taskInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
      addTask();
    }
  });

  filterBtns.forEach(btn => {
    btn.addEventListener('click', () => {
      filterBtns.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      currentFilter = btn.dataset.filter;
      renderTasks();
    });
  });

  sortSelect.addEventListener('change', (e) => {
    currentSort = e.target.value;
    renderTasks();
  });

  addTaskToggle.addEventListener('click', toggleInputSection);

  // 导出导入功能
  const exportBtn = document.getElementById('exportBtn');
  const importBtn = document.getElementById('importBtn');
  const importFile = document.getElementById('importFile');

  exportBtn.addEventListener('click', exportData);
  importBtn.addEventListener('click', () => importFile.click());
  importFile.addEventListener('change', importData);
}

// 切换输入区域显示/隐藏
function toggleInputSection() {
  const inputSection = document.getElementById('inputSection');
  const addTaskToggle = document.getElementById('addTaskToggle');
  
  inputSection.classList.toggle('collapsed');
  
  if (inputSection.classList.contains('collapsed')) {
    addTaskToggle.querySelector('span').textContent = '+ 添加新任务';
  } else {
    addTaskToggle.querySelector('span').textContent = '- 收起';
    document.getElementById('taskInput').focus();
  }
}

// 设置右键菜单
function setupContextMenu() {
  document.addEventListener('click', () => {
    hideContextMenu();
  });

  document.addEventListener('contextmenu', (e) => {
    const taskItem = e.target.closest('.task-item');
    if (taskItem && !e.target.closest('.task-checkbox')) {
      e.preventDefault();
      showContextMenu(e.pageX, e.pageY, taskItem.dataset.id);
    }
  });
}

// 显示右键菜单
function showContextMenu(x, y, taskId) {
  contextMenuTargetId = parseInt(taskId);
  
  let contextMenu = document.getElementById('contextMenu');
  if (!contextMenu) {
    contextMenu = document.createElement('div');
    contextMenu.id = 'contextMenu';
    contextMenu.className = 'context-menu';
    contextMenu.innerHTML = `
      <div class="context-menu-item edit" data-action="edit">✏️ 编辑</div>
      <div class="context-menu-item delete" data-action="delete">🗑️ 删除</div>
    `;
    document.body.appendChild(contextMenu);

    contextMenu.querySelectorAll('.context-menu-item').forEach(item => {
      item.addEventListener('click', (e) => {
        e.stopPropagation();
        const action = item.dataset.action;
        if (action === 'edit') {
          editTask(contextMenuTargetId);
        } else if (action === 'delete') {
          deleteTask(contextMenuTargetId);
        }
        hideContextMenu();
      });
    });
  }

  contextMenu.style.left = x + 'px';
  contextMenu.style.top = y + 'px';
  contextMenu.classList.add('show');
}

// 隐藏右键菜单
function hideContextMenu() {
  const contextMenu = document.getElementById('contextMenu');
  if (contextMenu) {
    contextMenu.classList.remove('show');
  }
}

// 从本地存储加载任务
function loadTasks() {
  chrome.storage.local.get(['tasks'], (result) => {
    if (result.tasks) {
      tasks = result.tasks;
      renderTasks();
      updateStats();
    }
  });
}

// 保存任务到本地存储
function saveTasks() {
  chrome.storage.local.set({ tasks: tasks }, () => {
    updateStats();
  });
}

// 添加新任务
function addTask() {
  const taskInput = document.getElementById('taskInput');
  const priorityInput = document.getElementById('priorityInput');
  const timeInput = document.getElementById('timeInput');
  const deadlineInput = document.getElementById('deadlineInput');

  const text = taskInput.value.trim();
  const priority = parseInt(priorityInput.value);
  const estimatedTime = parseFloat(timeInput.value);
  const deadline = deadlineInput.value;

  if (!text) {
    alert('请输入任务内容！');
    return;
  }

  if (isNaN(priority) || priority < 0 || priority > 5) {
    alert('优先级必须在0-5之间！');
    return;
  }

  if (isNaN(estimatedTime) || estimatedTime < 0) {
    alert('请输入有效的估计时间！');
    return;
  }

  const newTask = {
    id: Date.now(),
    text: text,
    priority: priority,
    estimatedTime: estimatedTime,
    deadline: deadline || null,
    completed: false,
    createdAt: new Date().toISOString()
  };

  tasks.unshift(newTask);
  saveTasks();
  renderTasks();

  // 清空输入并收起
  taskInput.value = '';
  priorityInput.value = '0';
  timeInput.value = '1';
  deadlineInput.value = '';
  toggleInputSection();
}

// 渲染任务列表
function renderTasks() {
  const taskList = document.getElementById('taskList');
  taskList.innerHTML = '';

  let filteredTasks = tasks;
  
  if (currentFilter === 'pending') {
    filteredTasks = tasks.filter(task => !task.completed);
  } else if (currentFilter === 'completed') {
    filteredTasks = tasks.filter(task => task.completed);
  }

  if (filteredTasks.length === 0) {
    taskList.innerHTML = '<li class="empty-state">暂无任务</li>';
    return;
  }

  // 排序
  if (currentSort === 'priority') {
    filteredTasks.sort((a, b) => b.priority - a.priority);
  } else if (currentSort === 'time') {
    filteredTasks.sort((a, b) => a.estimatedTime - b.estimatedTime);
  } else if (currentSort === 'deadline') {
    filteredTasks.sort((a, b) => {
      if (!a.deadline && !b.deadline) return 0;
      if (!a.deadline) return 1;
      if (!b.deadline) return -1;
      return new Date(a.deadline) - new Date(b.deadline);
    });
  }

  filteredTasks.forEach(task => {
    const li = createTaskElement(task);
    taskList.appendChild(li);
  });
}

// 创建任务元素
function createTaskElement(task) {
  const li = document.createElement('li');
  li.className = `task-item ${task.completed ? 'completed' : ''}`;
  li.dataset.id = task.id;

  const checkbox = document.createElement('input');
  checkbox.type = 'checkbox';
  checkbox.className = 'task-checkbox';
  checkbox.checked = task.completed;
  checkbox.addEventListener('change', (e) => {
    e.stopPropagation();
    toggleTask(task.id);
  });

  const content = document.createElement('div');
  content.className = 'task-content';

  const text = document.createElement('div');
  text.className = 'task-text';
  text.textContent = task.text;

  const meta = document.createElement('div');
  meta.className = 'task-meta';
  
  const priorityBadge = document.createElement('span');
  priorityBadge.className = `priority-badge priority-${task.priority}`;
  priorityBadge.textContent = `P${task.priority}`;
  
  const timeBadge = document.createElement('span');
  timeBadge.textContent = `⏱ ${task.estimatedTime}h`;

  meta.appendChild(priorityBadge);
  meta.appendChild(timeBadge);

  if (task.deadline) {
    const deadlineDate = new Date(task.deadline);
    const now = new Date();
    const isOverdue = deadlineDate < now && !task.completed;
    const isUrgent = !isOverdue && (deadlineDate - now) < 24 * 60 * 60 * 1000;
    
    const deadlineBadge = document.createElement('span');
    deadlineBadge.className = isOverdue ? 'deadline-overdue' : (isUrgent ? 'deadline-urgent' : 'deadline-normal');
    
    const options = { month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' };
    deadlineBadge.textContent = `📅 ${deadlineDate.toLocaleDateString('zh-CN', options)}`;
    
    meta.appendChild(deadlineBadge);
  }

  content.appendChild(text);
  content.appendChild(meta);

  li.appendChild(checkbox);
  li.appendChild(content);

  return li;
}

// 切换任务完成状态
function toggleTask(id) {
  const task = tasks.find(t => t.id === id);
  if (task) {
    task.completed = !task.completed;
    saveTasks();
    renderTasks();
  }
}

// 编辑任务
function editTask(id) {
  const task = tasks.find(t => t.id === id);
  if (!task) return;

  const li = document.querySelector(`[data-id="${id}"]`);
  if (!li) return;

  li.innerHTML = '';

  const editMode = document.createElement('div');
  editMode.className = 'edit-mode';

  const editInput = document.createElement('input');
  editInput.type = 'text';
  editInput.className = 'edit-input';
  editInput.value = task.text;

  const editRow = document.createElement('div');
  editRow.className = 'edit-row';

  const priorityInput = document.createElement('input');
  priorityInput.type = 'number';
  priorityInput.min = '0';
  priorityInput.max = '5';
  priorityInput.placeholder = '优先级(0-5)';
  priorityInput.value = task.priority;

  const timeInput = document.createElement('input');
  timeInput.type = 'number';
  timeInput.min = '0';
  timeInput.step = '0.5';
  timeInput.placeholder = '时间(小时)';
  timeInput.value = task.estimatedTime;

  editRow.appendChild(priorityInput);
  editRow.appendChild(timeInput);

  const deadlineEditRow = document.createElement('div');
  deadlineEditRow.className = 'edit-row';
  
  const deadlineInput = document.createElement('input');
  deadlineInput.type = 'datetime-local';
  deadlineInput.placeholder = '完成期限';
  deadlineInput.value = task.deadline || '';
  deadlineEditRow.appendChild(deadlineInput);

  const saveCancelBtns = document.createElement('div');
  saveCancelBtns.className = 'save-cancel-btns';

  const saveBtn = document.createElement('button');
  saveBtn.className = 'save-btn';
  saveBtn.textContent = '保存';
  saveBtn.addEventListener('click', () => {
    const newText = editInput.value.trim();
    const newPriority = parseInt(priorityInput.value);
    const newTime = parseFloat(timeInput.value);
    const newDeadline = deadlineInput.value;

    if (!newText) {
      alert('任务内容不能为空！');
      return;
    }

    if (isNaN(newPriority) || newPriority < 0 || newPriority > 5) {
      alert('优先级必须在0-5之间！');
      return;
    }

    if (isNaN(newTime) || newTime < 0) {
      alert('请输入有效的估计时间！');
      return;
    }

    task.text = newText;
    task.priority = newPriority;
    task.estimatedTime = newTime;
    task.deadline = newDeadline || null;
    saveTasks();
    renderTasks();
  });

  const cancelBtn = document.createElement('button');
  cancelBtn.className = 'cancel-btn';
  cancelBtn.textContent = '取消';
  cancelBtn.addEventListener('click', () => {
    renderTasks();
  });

  saveCancelBtns.appendChild(saveBtn);
  saveCancelBtns.appendChild(cancelBtn);

  editMode.appendChild(editInput);
  editMode.appendChild(editRow);
  editMode.appendChild(deadlineEditRow);
  editMode.appendChild(saveCancelBtns);

  li.appendChild(editMode);
  editInput.focus();
}

// 删除任务
function deleteTask(id) {
  if (confirm('确定要删除这个任务吗？')) {
    tasks = tasks.filter(t => t.id !== id);
    saveTasks();
    renderTasks();
  }
}

// 更新统计信息
function updateStats() {
  const statsElement = document.getElementById('stats');
  const total = tasks.length;
  const completed = tasks.filter(t => t.completed).length;
  const pending = total - completed;
  const totalHours = tasks.reduce((sum, task) => sum + task.estimatedTime, 0);
  const completedHours = tasks.filter(t => t.completed).reduce((sum, task) => sum + task.estimatedTime, 0);

  statsElement.textContent = `总计: ${total} | 已完成: ${completed} | 待完成: ${pending} | 总时间: ${totalHours}h | 已用时: ${completedHours}h`;
}

// 导出数据
function exportData() {
  const data = {
    tasks: tasks,
    exportDate: new Date().toISOString(),
    version: '1.0'
  };

  const jsonStr = JSON.stringify(data, null, 2);
  const blob = new Blob([jsonStr], { type: 'application/json' });
  const url = URL.createObjectURL(blob);
  
  const a = document.createElement('a');
  a.href = url;
  a.download = `todo_backup_${new Date().toISOString().slice(0, 10)}.json`;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
  
  alert('数据导出成功！');
}

// 导入数据
function importData(event) {
  const file = event.target.files[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const data = JSON.parse(e.target.result);
      
      if (!data.tasks || !Array.isArray(data.tasks)) {
        alert('无效的数据格式！');
        return;
      }

      if (confirm(`即将导入 ${data.tasks.length} 个任务，这将覆盖现有数据。确定继续吗？`)) {
        tasks = data.tasks;
        saveTasks();
        renderTasks();
        alert('数据导入成功！');
      }
    } catch (error) {
      alert('导入失败：' + error.message);
    }
    
    // 清空文件输入
    event.target.value = '';
  };
  
  reader.readAsText(file);
}
